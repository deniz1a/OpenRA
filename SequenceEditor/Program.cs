﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using OpenRa.FileFormats;
using System.Xml;
using System.Drawing;
using System.Drawing.Imaging;

namespace SequenceEditor
{
	static class Program
	{
		public static string UnitName;
		public static XmlDocument Doc;
		public static Dictionary<string, Bitmap[]> Shps = new Dictionary<string, Bitmap[]>();
		public static Palette Pal;
		public static Dictionary<string, Sequence> Sequences = new Dictionary<string, Sequence>();

		public static Bitmap[] LoadAndResolve( string shp )
		{
			try
			{
				var reader = new ShpReader(FileSystem.OpenWithExts(shp, ".shp", ".tem", ".sno", ".int"));
				return reader.Select(ih =>
					{
						var bmp = new Bitmap(reader.Width, reader.Height);
						for (var j = 0; j < bmp.Height; j++)
							for (var i = 0; i < bmp.Width; i++)
								bmp.SetPixel(i, j, Pal.GetColor(ih.Image[j * bmp.Width + i]));
						return bmp;
					}).ToArray();
			}
			catch
			{
				return new Bitmap[] { };
			}
		}

		public static void Save()
		{
			var e = Doc.SelectSingleNode(string.Format("//unit[@name=\"{0}\"]", UnitName)) as XmlElement;
			if (e == null)
			{
				e = Doc.CreateElement("unit");
				e.SetAttribute( "name", UnitName );
				e = Doc.SelectSingleNode("sequences").AppendChild(e) as XmlElement;
			}

			while (e.HasChildNodes) e.RemoveChild(e.FirstChild);	/* what a fail */

			foreach (var s in Sequences)
			{
				var seqnode = Doc.CreateElement("sequence");
				seqnode.SetAttribute("name", s.Key);
				seqnode.SetAttribute("start", s.Value.start.ToString());
				seqnode.SetAttribute("length", s.Value.length.ToString());
				if (s.Value.shp != UnitName)
					seqnode.SetAttribute("src", s.Value.shp);

				e.AppendChild(seqnode);
			}

			Doc.Save("sequences.xml");
		}

		[STAThread]
		static void Main( string[] args )
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			FileSystem.Mount(new Folder("./"));
			var packages = new[] { "redalert", "conquer", "hires", "general", "local", "temperat" };

			foreach( var p in packages )
				FileSystem.Mount( new Package( p + ".mix" ));

			Doc = new XmlDocument(); 
			Doc.Load("sequences.xml");

			Pal = new Palette(FileSystem.Open("temperat.pal"));

			UnitName = args.FirstOrDefault();
			if (UnitName == null)
				UnitName = GetTextForm.GetString("Unit to edit?", "e1");
			if (UnitName == null)
				return;

			var xpath = string.Format("//unit[@name=\"{0}\"]/sequence", UnitName);
			foreach (XmlElement e in Doc.SelectNodes(xpath))
			{
				if (e.HasAttribute("src"))
				{
					var src = e.GetAttribute("src");
					if (!Shps.ContainsKey(src))
						Shps[src] = LoadAndResolve(src);
				}
				Sequences[e.GetAttribute("name")] = new Sequence(e);
			}

			Application.Run(new Form1());
		}
	}

	class Sequence
	{
		public int start;
		public int length;
		public string shp;

		public Sequence(XmlElement e)
		{
			start = int.Parse(e.GetAttribute("start"));
			shp = e.GetAttribute("src");
			if (shp == "") shp = Program.UnitName;
			var a = e.GetAttribute("length");

			length = (a == "*")
				? Program.Shps[shp].Length - start
				: ((a == "") ? 1 : int.Parse(a));
		}

		public Sequence() { }
	}
}

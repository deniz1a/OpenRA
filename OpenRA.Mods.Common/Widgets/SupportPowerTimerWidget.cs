#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class SupportPowerTimerWidget : Widget
	{
		[Translate] public readonly string NoTeamText = "No Team";
		[Translate] public readonly string TeamText = "Team {0}";

		readonly string fontName = "Bold";
		readonly SpriteFont font;
		readonly float yIncrement, powerPos, timePos;

		readonly Dictionary<Player, SupportPowerManager> spManagers;
		readonly SortedDictionary<int, Player[]> playersByTeam;

		readonly bool noPowers;
		readonly World world;

		bool showTeamLabel, showPlayer, flashing;
		Color timerColor;

		[ObjectCreator.UseCtor]
		public SupportPowerTimerWidget(World world)
		{
			var actorsWithSPM = world.ActorsWithTrait<SupportPowerManager>()
				.Where(tp => !tp.Actor.IsDead && !tp.Actor.Owner.NonCombatant && tp.Actor.Owner != world.LocalPlayer);

			spManagers = new Dictionary<Player, SupportPowerManager>();
			foreach (var traitPair in actorsWithSPM)
				spManagers[traitPair.Actor.Owner] = traitPair.Trait;

			var availablePowers = world.Map.Rules.Actors.Values
				.SelectMany(ai => ai.Traits.WithInterface<SupportPowerInfo>())
				.Where(i => i.DisplayTimer).ToArray();

			noPowers = spManagers.Count == 0 || availablePowers.Length == 0;
			if (noPowers)
				return;

			playersByTeam = new SortedDictionary<int, Player[]>();
			foreach (var team in spManagers.Keys.GroupBy(p => world.LobbyInfo.ClientWithIndex(p.ClientIndex).Team))
				playersByTeam[team.Key] = team.ToArray();

			font = Game.Renderer.Fonts[fontName];
			yIncrement = font.Measure(" ").Y + 5;
			var padding = font.Measure("  ").X;
			var maxPlayerWidth = spManagers.Keys.Max(p => font.Measure(p.PlayerName).X);
			var maxPowerWidth = availablePowers.Max(i => font.Measure(i.Description).X);
			powerPos = maxPlayerWidth + padding;
			timePos = powerPos + maxPowerWidth + padding;

			this.world = world;
		}

		public override void Draw()
		{
			if (!IsVisible() || noPowers)
				return;

			var widgetBounds = new float2(Bounds.Location);
			var position = new float2(0, 0);

			foreach (var team in playersByTeam)
			{
				showTeamLabel = true;
				foreach (var player in team.Value)
				{
					var powers = spManagers[player].Powers.Values
						.Where(i => i.Instances.Any() && i.Info.DisplayTimer && !i.Disabled &&
							(i.Info.DisplayTimerToEnemies || player.IsAlliedWith(world.RenderPlayer))).ToArray();

					if (powers.Length == 0)
						continue;

					if (showTeamLabel)
					{
						var teamLabel = team.Key == 0 ? NoTeamText : string.Format(TeamText, team.Key);
						font.DrawTextWithContrast(teamLabel, widgetBounds + position, Color.White, Color.Black, 1);
						position.Y += yIncrement;
						showTeamLabel = false;
					}

					showPlayer = true;
					flashing = Game.LocalTick % 50 < 25;
					foreach (var power in powers)
					{
						if (showPlayer)
						{
							font.DrawTextWithContrast(player.PlayerName, widgetBounds + position, player.Color.RGB, Color.Black, 1);
							showPlayer = false;
						}

						timerColor = power.Ready && flashing ? Color.White : player.Color.RGB;

						position.X = powerPos;
						font.DrawTextWithContrast(power.Info.Description, widgetBounds + position, timerColor, Color.Black, 1);

						position.X = timePos;
						var remainingTime = WidgetUtils.FormatTime(power.RemainingTime, false);
						font.DrawTextWithContrast(remainingTime, widgetBounds + position, timerColor, Color.Black, 1);

						position.X = 0;
						position.Y += yIncrement;
					}
				}
			}
		}
	}
}

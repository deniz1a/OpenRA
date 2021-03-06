#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Hides the entire map in shroud.")]
	class HideMapCrateActionInfo : CrateActionInfo
	{
		public override object Create(ActorInitializer init) { return new HideMapCrateAction(init.Self, this); }
	}

	class HideMapCrateAction : CrateAction
	{
		public HideMapCrateAction(Actor self, HideMapCrateActionInfo info)
			: base(self, info) { }

		public override int GetSelectionShares(Actor collector)
		{
			// don't ever hide the map for people who have GPS.
			if (collector.Owner.HasFogVisibility)
				return 0;

			return base.GetSelectionShares(collector);
		}

		public override void Activate(Actor collector)
		{
			collector.Owner.Shroud.ResetExploration();
			base.Activate(collector);
		}
	}
}

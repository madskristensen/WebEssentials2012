using Microsoft.CSS.Core;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace MadsKristensen.EditorExtensions
{
    [Export(typeof(ICssItemChecker))]
    [Name("ZeroUnitErrorTagProvider")]
    [Order(After = "Default Declaration")]
    internal class ZeroUnitErrorTagProvider : ICssItemChecker
    {
        public ItemCheckResult CheckItem(ParseItem item, ICssCheckerContext context)
        {
            if (!WESettings.GetBoolean(WESettings.Keys.ValidateZeroUnit))
                return ItemCheckResult.Continue;

            NumericalValue number = (NumericalValue)item;
            UnitValue unit = number as UnitValue;

            if (unit == null || context == null)
                return ItemCheckResult.Continue;

            if (number.Number.Text == "0" && unit.UnitType != UnitType.Unknown && unit.UnitType != UnitType.Time)
            {
                string message = string.Format(Resources.BestPracticeZeroUnit, unit.UnitToken.Text);
                context.AddError(new SimpleErrorTag(number, message));
            }

            return ItemCheckResult.Continue;
        }

        public IEnumerable<Type> ItemTypes
        {
            get { return new[] { typeof(NumericalValue) }; }
        }
    }
}
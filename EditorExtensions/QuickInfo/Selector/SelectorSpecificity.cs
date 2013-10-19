using Microsoft.CSS.Core;
using System.Linq;

namespace MadsKristensen.EditorExtensions
{
    internal class SelectorSpecificity
    {
        private readonly Selector _selector;

        public SelectorSpecificity(Selector selector)
        {
            _selector = selector;
            Calculate();
        }

        public int IDs { get; set; }
        public int Classes { get; set; }
        public int Elements { get; set; }
        public int PseudoClasses { get; set; }
        public int PseudoElements { get; set; }
        public int Attributes { get; set; }

        public override string ToString()
        {
            return IDs + ", " + (Classes + PseudoClasses + Attributes) + ", " + (Elements + PseudoElements);
        }

        private void Calculate()
        {
            // IDs
            var visitorIDs = new CssItemCollector<IdSelector>();
            _selector.Accept(visitorIDs);

            if (visitorIDs.Items.Count > 0)
                IDs = visitorIDs.Items.Count;// *100;

            // Classes
            var visitorClasses = new CssItemCollector<ClassSelector>();
            _selector.Accept(visitorClasses);

            if (visitorClasses.Items.Count > 0)
                Classes = visitorClasses.Items.Count;// *10;

            // Attributes
            var visitorAttribute = new CssItemCollector<AttributeSelector>();
            _selector.Accept(visitorAttribute);

            if (visitorAttribute.Items.Count > 0)
                Attributes = visitorAttribute.Items.Count;// *10;

            // Elements
            var visitorElements = new CssItemCollector<ItemName>();
            _selector.Accept(visitorElements);
            Elements = visitorElements.Items.Count(i => i.Text != "*" && i.FindType<AttributeSelector>() == null);

            // Pseudo Elements
            var visitorPseudoElementSelector = new CssItemCollector<PseudoElementSelector>();
            _selector.Accept(visitorPseudoElementSelector);

            var visitorPseudoElementFunctionSelector = new CssItemCollector<PseudoElementFunctionSelector>();
            _selector.Accept(visitorPseudoElementFunctionSelector);

            PseudoElements = visitorPseudoElementSelector.Items.Count + visitorPseudoElementFunctionSelector.Items.Count;

            // Pseudo Classes
            var visitorPseudoClassSelector = new CssItemCollector<PseudoClassSelector>();
            _selector.Accept(visitorPseudoClassSelector);

            var visitorPseudoClassFunctionSelector = new CssItemCollector<PseudoClassFunctionSelector>(true);
            _selector.Accept(visitorPseudoClassFunctionSelector);

            int pseudoClases = visitorPseudoClassSelector.Items.Count(p => !p.IsPseudoElement());
            pseudoClases += visitorPseudoClassFunctionSelector.Items.Count(p => !p.Text.StartsWith(":not(") && !p.Text.StartsWith(":matches("));
            Elements += visitorPseudoClassSelector.Items.Count(p => p.IsPseudoElement());

            if (pseudoClases > 0)
                PseudoClasses = pseudoClases;// *10;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class EquationDivideOperator : EquationElement,IEquationOperator {
        public List<EquationElement> NumeratorValues { get; set; }
        public List<EquationElement> DenominatorValues { get; set; }

        public EquationDivideOperator(List<EquationElement> numeratorValues,List<EquationElement> denominatorValues) {
            NumeratorValues = numeratorValues;
            DenominatorValues = denominatorValues;
        }

        private double? SumElements(EquationElement element) {
            if (element is not IEquationOperator)
                return ( element as NumberElement ).Number;
            else
                return ( element as IEquationOperator ).GetOperatorResultIfPossible();
        }

        public double? GetOperatorResultIfPossible() {
            if (NumeratorValues.All(element => element is not UnknownElement) && DenominatorValues.All(element => element is not UnknownElement)) {
                List<EquationElement> orderedNumeratorValues = NumeratorValues.OrderByDescending(element => element is IEquationOperator ? 1 : 0).ToList();
                List<EquationElement> orderedDenominatorValues = DenominatorValues.OrderByDescending(element => element is IEquationOperator ? 1 : 0).ToList();

                double? resultNumerator = orderedNumeratorValues.Sum(SumElements);
                double? resultDenominator = orderedDenominatorValues.Sum(SumElements);

                return resultNumerator / resultDenominator;
            }
            else
                return null;
        }

        public override bool Equals(object obj) {
            if(obj is EquationDivideOperator) {
                EquationDivideOperator equationDivideOperator = obj as EquationDivideOperator;
                return equationDivideOperator.NumeratorValues.SequenceEqual(NumeratorValues) && equationDivideOperator.DenominatorValues.SequenceEqual(DenominatorValues);
            }
            return false;
        }
    }
}

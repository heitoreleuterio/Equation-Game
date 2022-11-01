using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class EquationMultiplyOperator : EquationElement,IEquationOperator{
        public List<EquationElement> AffectedValues { get; set; }
        public EquationMultiplyOperator(List<EquationElement> affectedValues) {
            AffectedValues = affectedValues;
        }

        public double? GetOperatorResultIfPossible() {
            if (AffectedValues.All(value => value is UnknownElement)) {
                double? result = 0;
                List<EquationElement> orderedAffectedValues = AffectedValues.OrderByDescending(element => element is IEquationOperator ? 1 : 0).ToList();
                for (int c = 0; c < AffectedValues.Count; c++) {
                    EquationElement equationElement = AffectedValues[c];
                    if (equationElement is not IEquationOperator) {
                        NumberElement numberElement = equationElement as NumberElement;
                        result *= numberElement.Number;
                    }
                    else {
                        IEquationOperator equationOperator = equationElement as IEquationOperator;
                        result *= equationOperator.GetOperatorResultIfPossible();
                    }
                }
                return result;
            }
            else
                return null;
        }

        public override bool Equals(object obj) {
            if(obj is EquationMultiplyOperator) {
                EquationMultiplyOperator equationMultiplyOperator = obj as EquationMultiplyOperator;
                return equationMultiplyOperator.AffectedValues.SequenceEqual(AffectedValues);
            }
            return false;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class Equation {
        private List<EquationElement> _firstSideElements;
        private List<EquationElement> _secondSideElements;
        public List<EquationElement> FirstSideElements {
            get => _firstSideElements;
            init => _firstSideElements = value;
        }
        public List<EquationElement> SecondSideElements {
            get => _secondSideElements;
            init => _secondSideElements = value;
        }

        public Equation(List<EquationElement> firstSideElements,List<EquationElement> secondeSideElements) {
            FirstSideElements = firstSideElements;
            SecondSideElements = secondeSideElements;
        }

        public bool PassFirstSideElementsToSecondSide(List<int> elements,PossibleOperations selectedOperation,bool isUnknownElementCoefficient) {
            return PassElementsFromOneSideToOther(ref _firstSideElements,ref _secondSideElements,elements,selectedOperation,isUnknownElementCoefficient);
        }

        public bool PassSecondSideElementsToFirstSide(List<int> elements,PossibleOperations selectedOperation,bool isUnknownElementCoefficient) {
            return PassElementsFromOneSideToOther(ref _secondSideElements,ref _firstSideElements,elements,selectedOperation,isUnknownElementCoefficient);
        }

        private bool PassElementsFromOneSideToOther(ref List<EquationElement> dragSide,ref List<EquationElement> dropSide,List<int> elements,PossibleOperations selectedOperation,bool isUnknownCoefficient) {
            bool success;
            IEnumerable<EquationElement> elementsDrag = dragSide
                    .Where((element,index) => elements.Contains(index))
                    .OrderByDescending(element => element is EquationMultiplyOperator || element is EquationDivideOperator);
            List<EquationElement> finalOrderElements = elementsDrag.ToList();
            if (elementsDrag.Any(element => element is EquationMultiplyOperator)) {
                success = selectedOperation == PossibleOperations.Divide;
                if (success) {
                    EquationMultiplyOperator multiplyOperator = elementsDrag
                    .Single(element => element is EquationMultiplyOperator) as EquationMultiplyOperator;

                    finalOrderElements[finalOrderElements.IndexOf(multiplyOperator)] = new EquationDivideOperator(dropSide,elementsDrag.ToList());
                }
            }
            else if (elementsDrag.Any(element => element is EquationMultiplyOperator)) {
                success = selectedOperation == PossibleOperations.Multiply;
                if (success) {
                    EquationDivideOperator divideOperator = elementsDrag
                    .Single(element => element is EquationDivideOperator) as EquationDivideOperator;

                    List<EquationElement> affectedValues = new List<EquationElement>();
                    affectedValues.AddRange(dropSide);
                    affectedValues.AddRange(elementsDrag.ToList());
                    finalOrderElements[finalOrderElements.IndexOf(divideOperator)] = new EquationMultiplyOperator(affectedValues);
                }
            }
            else if (elementsDrag.Any(element => element is NumberElement && element is not UnknownElement)) {
                NumberElement numberElement = elementsDrag.Single(element => element is NumberElement && element is not UnknownElement) as NumberElement;
                if (numberElement.Number >= 0)
                    success = selectedOperation == PossibleOperations.Subtract;
                else
                    success = selectedOperation == PossibleOperations.Sum;

                if (success)
                    finalOrderElements[finalOrderElements.IndexOf(numberElement)] = new NumberElement(numberElement.Number * -1);
            }
            else {
                UnknownElement unknownElement = elementsDrag.Single(element => element is UnknownElement) as UnknownElement;
                int index = finalOrderElements.IndexOf(unknownElement);
                if (isUnknownCoefficient) {
                    success = selectedOperation == PossibleOperations.Divide;

                    if (success) {
                        NumberElement numberElement = new NumberElement(unknownElement.Number);
                        EquationDivideOperator equationDivideOperator = new EquationDivideOperator(dropSide,new List<EquationElement> { numberElement });
                        finalOrderElements.Insert(index,equationDivideOperator);
                        finalOrderElements[index + 1] = numberElement;
                        dragSide[dragSide.IndexOf(unknownElement)] = new UnknownElement(unknownElement.Symbol,1);
                        elementsDrag = elementsDrag.Where(element => element is not UnknownElement);
                    }
                }
                else {
                    if (unknownElement.Number >= 0)
                        success = selectedOperation == PossibleOperations.Subtract;
                    else
                        success = selectedOperation == PossibleOperations.Sum;

                    if (success)
                        finalOrderElements[index] = new UnknownElement(unknownElement.Symbol,unknownElement.Number * -1);
                }
            }

            if (success) {
                dragSide.RemoveAll(element => elementsDrag.Contains(element));
                dropSide.AddRange(finalOrderElements);
            }

            return success;
        }

        public bool IsEquationFinished() {
            return FirstSideElements.Count == 1 && FirstSideElements[0] is UnknownElement && SecondSideElements.Count == 1 && SecondSideElements[0] is not UnknownElement;
        }

        public static Equation FromString(string textEquation) {
            string[] twoSideText = textEquation.Split("=");

            IEnumerable<string[]> firstSideText = DivideGroups(twoSideText[0],out List<int> firstSideGroupIndexes);
            IEnumerable<string[]> secondSideText = DivideGroups(twoSideText[1],out List<int> secondSideGroupIndexes);

            List<EquationElement> firstSideElements = TransformSideTextInEquationElements(firstSideText,firstSideGroupIndexes);
            List<EquationElement> secondSideElements = TransformSideTextInEquationElements(secondSideText,secondSideGroupIndexes);

            return new Equation(firstSideElements,secondSideElements);
        }

        private static IEnumerable<string[]> DivideGroups(string sideText,out List<int> elementGroupIndexes) {
            List<string> groupsDividedEquation = new List<string>();

            elementGroupIndexes = new List<int>();

            List<char> actualGroup = new List<char>();
            for (int c = 0; c < sideText.Length; c++) {
                char caracter = sideText[c];
                if (caracter == '(') {
                    if (actualGroup.Count > 0)
                        groupsDividedEquation.Add(new string(actualGroup.ToArray()));
                    actualGroup = new List<char>();
                }

                actualGroup.Add(caracter);

                if (caracter == ')') {
                    groupsDividedEquation.Add(new string(actualGroup.ToArray()));
                    elementGroupIndexes.Add(groupsDividedEquation.Count - 1);
                    actualGroup = new List<char>();
                }

            }

            if (actualGroup.Count > 0)
                groupsDividedEquation.Add(new string(actualGroup.ToArray()));

            groupsDividedEquation = groupsDividedEquation.Select(group => group.
            Replace("(","").
            Replace(")","").
            Replace("=","").
            Replace("+ ","+").
            Replace("- ","-")).
            ToList();

            return groupsDividedEquation.Select(group => group.Split(' ').Where(letter => letter != "").ToArray());

        }

        private static List<EquationElement> TransformSideTextInEquationElements(IEnumerable<string[]> sideText,List<int> elementGroupIndexes) {
            List<EquationElement> sideElements = new List<EquationElement>();
            for (int c = 0; c < sideText.Count(); c++) {
                string[] group = sideText.ElementAt(c);
                List<EquationElement> elements = new List<EquationElement>();
                for (int d = 0; d < group.Length; d++) {
                    string textElement = group[d];
                    char? symbol = null;
                    PossibleOperations? sign = null;
                    double? number = null;
                    foreach (char caracter in textElement) {
                        if (int.TryParse(caracter.ToString(),out int result)) {
                            number = int.Parse(number.ToString() + caracter.ToString());
                        }
                        else if (caracter == '.' || caracter == '/' || caracter == '÷')
                            sign = caracter == '.' ? PossibleOperations.Multiply : PossibleOperations.Divide;
                        else {
                            if (caracter != '+' && caracter != '-')
                                symbol = caracter;
                            else
                                sign = caracter == '-' ? PossibleOperations.Subtract : PossibleOperations.Sum;
                        }
                    }
                    EquationElement element;
                    if (symbol != null && number != null && sign != PossibleOperations.Multiply && sign != PossibleOperations.Divide) {
                        element = new UnknownElement(symbol.Value,sign == PossibleOperations.Subtract ? number.Value * -1 : number.Value);
                    }
                    else if (number != null) {
                        element = new NumberElement(sign == PossibleOperations.Subtract ? number.Value * -1 : number.Value);
                    }
                    else if (sign != null && ( sign == PossibleOperations.Multiply || sign == PossibleOperations.Divide )) {
                        if (sign == PossibleOperations.Multiply) {
                            List<EquationElement> affectedElements = new List<EquationElement>();
                            if (elements.Count == 0) {
                                affectedElements.Add(sideElements[sideElements.Count - 1]);
                            }
                            else {
                                affectedElements.Add(elements[d - 1]);
                            }
                            element = new EquationMultiplyOperator(affectedElements);
                        }
                        else {
                            element = new EquationDivideOperator(elements.Concat(sideElements.ToList()).ToList(),new List<EquationElement>());
                        }
                    }
                    else {
                        element = null;
                    }
                    if (elements.Count >= 1 && elements[d - 1] is EquationMultiplyOperator) {
                        EquationMultiplyOperator equationMultiplyOperator = elements[d - 1] as EquationMultiplyOperator;
                        equationMultiplyOperator.AffectedValues.Add(element);
                    }
                    else if (elements.Count >= 1 && elements[d - 1] is EquationDivideOperator) {
                        EquationDivideOperator equationDivideOperator = elements[d - 1] as EquationDivideOperator;
                        equationDivideOperator.DenominatorValues.Add(element);
                    }
                    elements.Add(element);
                }
                if (elementGroupIndexes.Contains(c)) {
                    EquationGroup equationGroup = new EquationGroup(elements);
                    if (sideElements[sideElements.Count - 1] is EquationMultiplyOperator) {
                        EquationMultiplyOperator equationMultiplyOperator = sideElements[sideElements.Count - 1] as EquationMultiplyOperator;
                        equationMultiplyOperator.AffectedValues.Add(equationGroup);
                    }
                    else if (sideElements[sideElements.Count - 1] is EquationDivideOperator) {
                        EquationDivideOperator equationDivideOperator = sideElements[sideElements.Count - 1] as EquationDivideOperator;
                        equationDivideOperator.DenominatorValues.Add(equationGroup);
                    }
                    sideElements.Add(equationGroup);
                }
                else
                    sideElements.AddRange(elements);
            };
            return sideElements;
        }


    }
}

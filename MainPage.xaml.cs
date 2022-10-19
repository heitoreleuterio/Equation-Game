using Jogo_Algebra.Resources.Entidades;
using System.Runtime.CompilerServices;

namespace Jogo_Algebra;

public partial class MainPage : ContentPage {
    private TaskCompletionSource<PossibleOperations> _actualOperationTask;
    private List<Tuple<ViewEquationElementType,Border>> ElementsToDrag = new List<Tuple<ViewEquationElementType,Border>>();
    private bool DragSuccess = false;
    private Dictionary<PossibleOperations,string> OperatorsSign = new Dictionary<PossibleOperations,string> {
        [PossibleOperations.Divide] = "÷",
        [PossibleOperations.Multiply] = ".",
        [PossibleOperations.Sum] = "+",
        [PossibleOperations.Subtract] = "-"
    };

    public MainPage() {
        InitializeComponent();
        List<EquationElement> firstSideEquationElements = new List<EquationElement>();
        UnknownElement unknownElement = new UnknownElement('x');
        NumberElement numberElement = new NumberElement(20);
        EquationMultiplyOperator binaryModifier = new EquationMultiplyOperator(new List<EquationElement> {
            numberElement,
            unknownElement
        });
        NumberElement numberElement4 = new NumberElement(50);
        firstSideEquationElements.Add(numberElement);
        firstSideEquationElements.Add(binaryModifier);
        firstSideEquationElements.Add(unknownElement);
        firstSideEquationElements.Add(numberElement4);
        List<EquationElement> secondSideEquationElements = new List<EquationElement>();
        NumberElement numberElement2 = new NumberElement(20);
        NumberElement numberElement3 = new NumberElement(5);
        secondSideEquationElements.Add(numberElement2);
        secondSideEquationElements.Add(numberElement3);
        Equation equation = new Equation(firstSideEquationElements,secondSideEquationElements);
        DisplayEquation(equation);
    }

    private void DragGestureRecognizer_DragStarting(object sender,DragStartingEventArgs e) {
        Border border = ( sender as GestureRecognizer ).Parent as Border;
        Label label = border.Content as Label;
        e.Data.Text = label.Text;
        e.Data.Properties.Add("object",border);
        border.Animate("animationTest",new Animation(v => border.Margin = new Thickness(15 * v)));
        border.ScaleTo(1.1,easing: Easing.SpringIn);
        DragSuccess = false;
    }

    private void LeftEqualDrop(object sender,DropEventArgs e) {
        Drop(e,leftEqual,rightEqual,ElementDirection.RightToLeft);
    }
    private void RightEqualDrop(object sender,DropEventArgs e) {
        Drop(e,rightEqual,leftEqual,ElementDirection.LeftToRight);
    }

    private async void Drop(DropEventArgs e,HorizontalStackLayout dropStackLayout,HorizontalStackLayout dragStackLayout,ElementDirection direction) {
        if (ElementsToDrag.All(tuple => !dropStackLayout.Contains(tuple.Item2))) {
            DragSuccess = true;
            _actualOperationTask = new TaskCompletionSource<PossibleOperations>();
            selectNewOperationPopup.IsVisible = true;
            PossibleOperations selectedOperation = await _actualOperationTask.Task;
            selectNewOperationPopup.IsVisible = false;
            List<int> indexToRemove = new List<int>();
            List<Tuple<Border,Border>> borders = new List<Tuple<Border,Border>>();
            foreach (Tuple<ViewEquationElementType,Border> tuple in ElementsToDrag) {
                Border originalBorder = tuple.Item2;
                if (!( !IsOperator(tuple.Item1) && ElementsToDrag.Any(actualTuple => IsOperator(actualTuple.Item1)) ))
                    originalBorder.IsVisible = true;
                Label originalLabel = originalBorder.Content as Label;
                string text = originalLabel.Text;
                string borderStyleKey;
                if (IsOperator(tuple.Item1)) {
                    borderStyleKey = selectedOperation == PossibleOperations.Sum || selectedOperation == PossibleOperations.Subtract ? "signBorderStyle" : "binaryOperatorBorderStyle";
                    Tuple<ViewEquationElementType,Border> signBorder = ElementsToDrag.SingleOrDefault(tuple => !IsOperator(tuple.Item1) && tuple.Item1 != ViewEquationElementType.EquationElement,null);
                    if (signBorder != null) {
                        bool showSignBorder = signBorder.Item1 == ViewEquationElementType.Subtract && ( selectedOperation != PossibleOperations.Subtract && selectedOperation != PossibleOperations.Sum );
                        signBorder.Item2.IsVisible = showSignBorder;
                    }
                }
                else {
                    borderStyleKey = tuple.Item1 == ViewEquationElementType.EquationElement ? "defaultBorderStyle" : ( selectedOperation == PossibleOperations.Sum || selectedOperation == PossibleOperations.Subtract ? "signBorderStyle" : "binaryOperatorBorderStyle" );
                }
                Border border = new Border {
                    Style = Resources.Single(resource => resource.Key == borderStyleKey).Value as Style
                };
                border.IsVisible = tuple.Item2.IsVisible;
                string newSign = selectedOperation == PossibleOperations.Sum
                    ? OperatorsSign[PossibleOperations.Sum]
                    : ( selectedOperation == PossibleOperations.Subtract
                    ? OperatorsSign[PossibleOperations.Subtract]
                    : ( selectedOperation == PossibleOperations.Multiply ? OperatorsSign[PossibleOperations.Multiply] : OperatorsSign[PossibleOperations.Divide] ) );
                string borderText;
                if (tuple.Item1 == ViewEquationElementType.Sum || tuple.Item1 == ViewEquationElementType.Subtract) {
                    if (ElementsToDrag.All(tuple => tuple.Item1 != ViewEquationElementType.Multiply && tuple.Item1 != ViewEquationElementType.Divide))
                        borderText = newSign;
                    else
                        borderText = text;
                }
                else if (tuple.Item1 != ViewEquationElementType.EquationElement)
                    borderText = newSign;
                else
                    borderText = text;
                border.Content = new Label {
                    Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                    Text = borderText
                };
                border.Opacity = 0;
                originalBorder.Scale = 1;
                originalBorder.Margin = 0;
                borders.Add(new Tuple<Border,Border>(originalBorder,border));
                dropStackLayout.Add(border);
            }
            foreach (Tuple<Border,Border> tuple in borders) {
                int index = dragStackLayout.IndexOf(tuple.Item1);
                indexToRemove.Add(index);
                int index2 = dropStackLayout.IndexOf(tuple.Item2);
                IEnumerable<IView> elementsSubsequent;
                IEnumerable<IView> elementsPrevious;
                bool isDirectionLeftToRight = direction == ElementDirection.LeftToRight;

                if (isDirectionLeftToRight) {
                    elementsSubsequent = dragStackLayout.Children.ToArray()[( index + 1 )..];
                    elementsPrevious = dropStackLayout.Children.ToArray()[..index2];
                }
                else {
                    elementsSubsequent = dragStackLayout.Children.ToArray()[..index];
                    elementsPrevious = dropStackLayout.Children.ToArray()[( index2 + 1 )..];
                }

                double subsequentMargin = elementsSubsequent.Sum(element => GetElementActualWidthIfVisibleOrReturnZero(element));
                double previousMargin = elementsPrevious.Sum(element => GetElementActualWidthIfVisibleOrReturnZero(element));
                double equalSignMeasures = equalSignElement.DesiredSize.Width;

                double borderMeasures = tuple.Item1.Measure(double.PositiveInfinity,double.PositiveInfinity).Request.Width;
                double totalTranslate = ( subsequentMargin + equalSignMeasures + previousMargin + borderMeasures + 40 + ( isDirectionLeftToRight ? 3 : 0 ) ) * ( isDirectionLeftToRight ? 1 : -1 );
                double halfTranslate = totalTranslate / 2;

                const double animationYMax = 150;
                tuple.Item1.Animate("dropCompletedTransitionAnimation",new Animation(v => {
                    double progress = totalTranslate * v;
                    double yProgress = 0;
                    if (( isDirectionLeftToRight && progress <= halfTranslate ) || ( !isDirectionLeftToRight && progress >= halfTranslate ))
                        yProgress = -1 * animationYMax * v;
                    else
                        yProgress = -1 * animationYMax + ( animationYMax * v );
                    tuple.Item1.TranslationX = progress;
                    tuple.Item1.TranslationY = yProgress;
                }),length: 1250,rate: 5,easing: Easing.CubicInOut,finished: (finalValue,canceled) => {
                    Thread.Sleep(300);
                    if (indexToRemove.Count == ElementsToDrag.Count) {
                        indexToRemove = indexToRemove.OrderBy(index => index).ToList();
                        for (int c = 0; c < indexToRemove.Count; c++)
                            dragStackLayout.RemoveAt(indexToRemove[c] - c);
                        ShowOrNotTheSign(dragStackLayout.ElementAt(0) as Border);
                        ShowOrNotTheSign(dropStackLayout.ElementAt(0) as Border);
                    }
                    tuple.Item2.Opacity = 1;
                    ElementsToDrag.Clear();
                });
            }
        }
    }

    private double GetElementActualWidthIfVisibleOrReturnZero(IView element) {
        if (( element as Border ).IsVisible) {
            Size elementSize = element.Measure(double.PositiveInfinity,double.PositiveInfinity);
            return elementSize.Width;
        }
        return 0;
    }

    private void ShowOrNotTheSign(Border borderSign) {
        string signText = ( borderSign.Content as Label ).Text;
        borderSign.IsVisible = signText != OperatorsSign[PossibleOperations.Sum];
    }

    private void DragGestureRecognizer_DropCompleted(object sender,DropCompletedEventArgs e) {
        if (!DragSuccess) {
            Border border = ( sender as GestureRecognizer ).Parent as Border;
            border.Animate("dropCompletedAnimation",new Animation(v => border.Margin = new Thickness(15 - ( 15 * v ))),length: 500);
            border.ScaleTo(1,easing: Easing.SpringOut,length: 500);
            ElementsToDrag.Clear();
        }
    }

    private void DisplayEquation(Equation equation) {
        leftEqual.Clear();
        rightEqual.Clear();
        for (int c = 0; c < equation.FirstSideElements.Count; c++)
            GenerateViewEquationElement(equation.FirstSideElements,leftEqual,c).ToList().ForEach(viewElement => leftEqual.Add(viewElement));
        for (int c = 0; c < equation.SecondSideElements.Count; c++)
            GenerateViewEquationElement(equation.SecondSideElements,rightEqual,c).ToList().ForEach(viewElement => rightEqual.Add(viewElement));

    }

    private IEnumerable<Border> GenerateViewEquationElement(List<EquationElement> equationElements,Layout layout,int index) {
        EquationElement equationElement = equationElements[index];
        Border border = new Border {
            Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
        };
        DragGestureRecognizer dragGestureRecognizer = new DragGestureRecognizer();
        border.GestureRecognizers.Add(dragGestureRecognizer);
        Label label = new Label {
            Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
        };
        string text;
        if (equationElement is UnknownElement) {
            UnknownElement unknownElement = equationElement as UnknownElement;
            text = unknownElement.Symbol.ToString();
            dragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                if (equationElements.Count > 1) {
                    int numberOfNumberElementsBeforeThis = equationElements.ToArray()[..index].Count(element => element is NumberElement && ( element as NumberElement ).Number < 0);
                    if (index != 0 && equationElements[index - 1] is IEquationOperator) {
                        IEquationOperator equationOperator = equationElements[index - 1] as IEquationOperator;
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) - 1) as Border));
                    }
                    else if (index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator) {
                        IEquationOperator equationOperator = equationElements[index + 1] as IEquationOperator;
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) + 1) as Border));
                    }
                }
                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(ViewEquationElementType.EquationElement,border));
                DragGestureRecognizer_DragStarting(sender,e);
            };
            dragGestureRecognizer.DropCompleted += DragGestureRecognizer_DropCompleted;
        }
        else if (equationElement is NumberElement) {
            NumberElement numberElement = ( equationElement as NumberElement );
            bool isPositive = numberElement.Number >= 0;
            Border borderSign = new Border {
                Style = Resources.Single(resource => resource.Key == "signBorderStyle").Value as Style
            };
            Label labelSign = new Label {
                Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
            };
            labelSign.Text = isPositive ? OperatorsSign[PossibleOperations.Sum] : OperatorsSign[PossibleOperations.Subtract];
            borderSign.Content = labelSign;
            borderSign.IsVisible = !( isPositive && index == 0 ) ? true : false;
            bool hasBinaryOperator = equationElements.Count > 1 && ( ( index != 0 && equationElements[index - 1] is IEquationOperator ) || ( index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator ) );
            dragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                if (equationElements.Count > 1) {
                    int numberOfNumberElementsBeforeThis = equationElements.ToArray()[..( index + 1 )].Count(element => element is NumberElement && ( element as NumberElement ).Number < 0);
                    if (index != 0 && equationElements[index - 1] is IEquationOperator) {
                        IEquationOperator equationOperator = equationElements[index - 1] as IEquationOperator;
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) - 1) as Border));
                    }
                    else if (index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator) {
                        IEquationOperator equationOperator = equationElements[index + 1] as IEquationOperator;
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) + 1) as Border));
                    }
                }
                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(isPositive ? ViewEquationElementType.Sum : ViewEquationElementType.Subtract,borderSign));
                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border>(ViewEquationElementType.EquationElement,border));
                DragGestureRecognizer_DragStarting(sender,e);
            };
            dragGestureRecognizer.DropCompleted += DragGestureRecognizer_DropCompleted;
            yield return borderSign;
            text = numberElement.Number.ToString().Replace("-","");
        }
        else {
            border.Style = Resources.Single(resource => resource.Key == "binaryOperatorBorderStyle").Value as Style;
            text = equationElement is EquationMultiplyOperator ? OperatorsSign[PossibleOperations.Multiply] : OperatorsSign[PossibleOperations.Divide];
        }
        label.Text = text;
        border.Content = label;
        yield return border;
    }

    private bool IsOperator(ViewEquationElementType type) {
        return type == ViewEquationElementType.Multiply || type == ViewEquationElementType.Divide;
    }

    private void Button_Clicked(object sender,EventArgs e) {
        _actualOperationTask.SetResult(PossibleOperations.Sum);
    }

    private void Button_Clicked_1(object sender,EventArgs e) {
        _actualOperationTask.SetResult(PossibleOperations.Subtract);
    }

    private void Button_Clicked_2(object sender,EventArgs e) {
        _actualOperationTask.SetResult(PossibleOperations.Multiply);
    }

    private void Button_Clicked_3(object sender,EventArgs e) {
        _actualOperationTask.SetResult(PossibleOperations.Divide);
    }
}


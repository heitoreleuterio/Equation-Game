using Jogo_Algebra.Resources.Entidades;
using Microsoft.Maui.Layouts;

namespace Jogo_Algebra;

public partial class MainPage : ContentPage {
    private TaskCompletionSource<PossibleOperations> _actualOperationTask;
    private List<Tuple<ViewEquationElementType,Border,EquationElement>> ElementsToDrag = new List<Tuple<ViewEquationElementType,Border,EquationElement>>();
    private bool DragSuccess = false;
    private Equation ActualEquation;
    private Dictionary<PossibleOperations,string> OperatorsSign = new Dictionary<PossibleOperations,string> {
        [PossibleOperations.Divide] = "÷",
        [PossibleOperations.Multiply] = ".",
        [PossibleOperations.Sum] = "+",
        [PossibleOperations.Subtract] = "-"
    };

    public MainPage() {
        InitializeComponent();
        Equation equation2 = Equation.FromString("10 - 8x + 2 = 5x - 8x + 2");
        ActualEquation = equation2;
        DisplayEquation(equation2);
        SizeChanged += MainPage_SizeChanged;
    }

    private void MainPage_SizeChanged(object sender,EventArgs e) {
        double measure = bordaPrincipal.Measure(double.PositiveInfinity,double.PositiveInfinity).Request.Width;
        DisplayEquation(ActualEquation);
        if (measure > 750) {
            bordaPrincipal.Content.WidthRequest = measure;
            bordaPrincipal.Content.Scale = 750 / measure;
        }
    }

    private void DragGestureRecognizer_DragStarting(object sender,DragStartingEventArgs e) {
        Border border = ( sender as GestureRecognizer ).Parent as Border;
        Label label = border.Content as Label;
        border.Animate("growAnimation",new Animation(v => { border.Margin = new Thickness(15 * v); border.StrokeThickness = 2.5 * v; }));
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
            bool isDirectionLeftToRight = direction == ElementDirection.LeftToRight;
            Tuple<ViewEquationElementType,Border,EquationElement> equationMultiplyOperator = ElementsToDrag.SingleOrDefault(tuple => tuple.Item1 == ViewEquationElementType.Multiply,null);
            bool restartElements = false;
            foreach (Tuple<ViewEquationElementType,Border,EquationElement> tuple in ElementsToDrag) {
                Border originalBorder = tuple.Item2;
                if (!( !IsOperator(tuple.Item1) && ElementsToDrag.Any(actualTuple => IsOperator(actualTuple.Item1)) ) && tuple.Item1 != ViewEquationElementType.UnknownElementMultiplierValue)
                    originalBorder.IsVisible = true;
                View originalContent = originalBorder.Content;
                string text = originalContent is Label ? ( originalContent as Label ).Text : null;
                if (IsOperator(tuple.Item1)) {
                    if (tuple.Item2.IsVisible || ( selectedOperation == PossibleOperations.Subtract )) {
                        tuple.Item2.IsVisible = true;
                        Tuple<ViewEquationElementType,Border,EquationElement> signBorder = ElementsToDrag.SingleOrDefault(tuple => !IsOperator(tuple.Item1) && tuple.Item1 != ViewEquationElementType.EquationElement && tuple.Item1 != ViewEquationElementType.UnknownElementMultiplierValue,null);
                        if (signBorder != null) {
                            bool showSignBorder = signBorder.Item1 == ViewEquationElementType.Subtract && ( selectedOperation != PossibleOperations.Subtract && selectedOperation != PossibleOperations.Sum );
                            signBorder.Item2.IsVisible = showSignBorder;
                        }
                    }
                }
                Border border = new Border {
                    Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
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
                else if (tuple.Item1 != ViewEquationElementType.EquationElement && tuple.Item1 != ViewEquationElementType.UnknownElementMultiplierValue)
                    borderText = newSign;
                else
                    borderText = text;
                if (( tuple.Item1 == ViewEquationElementType.Subtract || tuple.Item1 == ViewEquationElementType.Sum ) && ElementsToDrag.Any(tuple => tuple.Item1 == ViewEquationElementType.UnknownElementMultiplierValue)) {
                    Border copySignBorder = new Border();
                    copySignBorder.Style = originalBorder.Style;
                    Label copySignLabel = new Label();
                    copySignLabel.Style = originalContent.Style;
                    copySignLabel.Text = borderText;

                    copySignBorder.Content = copySignLabel;

                    copySignBorder.Opacity = 0;

                    border.Content = new Label {
                        Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                        Text = tuple.Item1 == ViewEquationElementType.Sum ? OperatorsSign[PossibleOperations.Sum] : OperatorsSign[PossibleOperations.Subtract]
                    };

                    borders.Add(new Tuple<Border,Border>(originalBorder,copySignBorder));
                    dropStackLayout.Add(copySignBorder);
                }
                else {
                    border.Content = text != null ? new Label {
                        Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                        Text = borderText
                    } : CloneEquationGroupViewContent(originalContent as HorizontalStackLayout);
                }
                border.Opacity = 0;
                if (tuple.Item1 == ViewEquationElementType.UnknownElementMultiplierValue) {
                    border.Margin = new Thickness(border.Margin.Left,border.Margin.Top,-16,border.Margin.Bottom);
                    border.ZIndex = 10;
                    border.Padding = new Thickness(border.Padding.Left,border.Padding.Top,0,border.Padding.Bottom);
                }
                if (tuple.Item1 == ViewEquationElementType.EquationElement && tuple.Item3 == null) {
                    border.Margin = new Thickness(border.Margin.Left,border.Margin.Top,-16,border.Margin.Bottom);
                    border.ZIndex = 10;
                    border.Padding = new Thickness(border.Padding.Left,border.Padding.Top,0,border.Padding.Bottom);
                }
                originalBorder.Scale = 1;
                originalBorder.Margin = 0;
                originalBorder.StrokeThickness = 0;
                borders.Add(new Tuple<Border,Border>(originalBorder,border));
                dropStackLayout.Add(border);
            }
            bordaPrincipal.Content.Scale = 1;
            bordaPrincipal.Content.WidthRequest = -1;
            double measure = bordaPrincipal.MeasureContent(double.PositiveInfinity,double.PositiveInfinity).Width;
            if (measure > 750) {
                bordaPrincipal.Content.WidthRequest = measure;
                bordaPrincipal.Content.Scale = 750 / measure;
            }
            foreach (Tuple<Border,Border> tuple in borders) {
                int index = dragStackLayout.IndexOf(tuple.Item1);
                if (!indexToRemove.Contains(index))
                    indexToRemove.Add(index);
                int index2 = dropStackLayout.IndexOf(tuple.Item2);
                IEnumerable<IView> elementsSubsequent;
                IEnumerable<IView> elementsPrevious;

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
                if (!tuple.Item1.AnimationIsRunning("dropCompletedTransitionAnimation")) {
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
                            
                            bool success;
                            bool isUnknownElementCoefficient = ElementsToDrag.Any(tuple => tuple.Item1 == ViewEquationElementType.UnknownElementMultiplierValue);
                            restartElements = true;
                            if (direction == ElementDirection.LeftToRight)
                                success = ActualEquation.PassFirstSideElementsToSecondSide(ElementsToDrag.Select(tuple => ActualEquation.FirstSideElements.IndexOf(tuple.Item3)).ToList(),selectedOperation,isUnknownElementCoefficient);
                            else
                                success = ActualEquation.PassSecondSideElementsToFirstSide(ElementsToDrag.Select(tuple => ActualEquation.SecondSideElements.IndexOf(tuple.Item3)).ToList(),selectedOperation,isUnknownElementCoefficient);
                            Color finalColor;
                            Color defaultColor = backgroundGrid.BackgroundColor;
                            if (success)
                                finalColor = Color.FromArgb("#4be572");
                            else
                                finalColor = Color.FromArgb("#ea4444");

                            double differenceOfRed = defaultColor.Red - finalColor.Red;
                            double differenceOfGreen = defaultColor.Green - finalColor.Green;
                            double differenceOfBlue = defaultColor.Blue - finalColor.Blue;
                            backgroundGrid.Animate("backgroundGridAnimation",new Animation(v => {
                                float finalRed = (float)( defaultColor.Red - ( differenceOfRed * ( v / 100 ) ) );
                                float finalGreen = (float)( defaultColor.Green - ( differenceOfGreen * ( v / 100 ) ) );
                                float finalBlue = (float)( defaultColor.Blue - ( differenceOfBlue * ( v / 100 ) ) );

                                backgroundGrid.BackgroundColor = new Color(finalRed,finalGreen,finalBlue);
                            },0,100,Easing.SpringIn),length: 999,finished: (canceled,finalValue) => {
                                Thread.Sleep(300);
                                double differenceOfRed = finalColor.Red - defaultColor.Red;
                                double differenceOfGreen = finalColor.Green - defaultColor.Green;
                                double differenceOfBlue = finalColor.Blue - defaultColor.Blue;

                                backgroundGrid.Animate("backgroundGridAnimationOut",new Animation(v => {
                                    float finalRed = (float)( finalColor.Red - ( differenceOfRed * v ) );
                                    float finalGreen = (float)( finalColor.Green - ( differenceOfGreen * v ) );
                                    float finalBlue = (float)( finalColor.Blue - ( differenceOfBlue * v ) );

                                    backgroundGrid.BackgroundColor = new Color(finalRed,finalGreen,finalBlue);
                                }),finished: (canceled,finalValue) => {
                                    if (!success || restartElements)
                                        DisplayEquation(ActualEquation);
                                    else {
                                        if (ActualEquation.IsEquationFinished()) {
                                            DisplayAlert("Alerta","Equação Finalizada","Ok");
                                        }
                                    }
                                    bordaPrincipal.Content.Scale = 1;
                                    bordaPrincipal.Content.WidthRequest = -1;
                                    double newMeasure = bordaPrincipal.MeasureContent(double.PositiveInfinity,double.PositiveInfinity).Width;
                                    if (newMeasure > 750) {
                                        double newScale = 750 / newMeasure;
                                        bordaPrincipal.Content.Scale = newScale;
                                        bordaPrincipal.Content.WidthRequest = newMeasure;
                                    }
                                    else {
                                        bordaPrincipal.Content.Scale = 1;
                                        bordaPrincipal.Content.WidthRequest = -1;
                                    }
                                });
                            });
                        }
                        borders.Where(actualTuple => actualTuple.Item1 == tuple.Item1).ToList().ForEach(tuple => tuple.Item2.Opacity = 1);
                        ElementsToDrag.Clear();
                    });
                }
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

    private Task DragGestureRecognizer_DropCompleted(object sender,DropCompletedEventArgs e) {
        TaskCompletionSource taskCompletionSource = new TaskCompletionSource();
        if (!DragSuccess) {
            Border border = ( sender as GestureRecognizer ).Parent as Border;
            border.Animate("dropCompletedAnimation",new Animation(v => { border.Margin = new Thickness(15 - ( 15 * v )); border.StrokeThickness = 2.5 - ( 2.5 * v ); }),length: 500,finished: ( (a,b) => {
                taskCompletionSource.SetResult();
            } ));
            border.ScaleTo(1,easing: Easing.SpringOut,length: 500);
            ElementsToDrag.Clear();
        }
        return taskCompletionSource.Task;
    }

    private void DisplayEquation(Equation equation) {
        leftEqual.Clear();
        rightEqual.Clear();

        for (int c = 0; c < equation.FirstSideElements.Count; c++)
            GenerateViewEquationElement(equation.FirstSideElements,c,leftEqual).ToList().ForEach(viewElement => {
                leftEqual.Add(viewElement);
            });

        for (int c = 0; c < equation.SecondSideElements.Count; c++)
            GenerateViewEquationElement(equation.SecondSideElements,c,rightEqual).ToList().ForEach(viewElement => {
                rightEqual.Add(viewElement);
            });
    }

    private IEnumerable<Border> GenerateViewEquationElement(List<EquationElement> equationElements,int index = 0,Layout layout = null) {
        EquationElement equationElement = equationElements[index];
        Border border = new Border {
            Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
        };
        DragGestureRecognizer dragGestureRecognizer = new DragGestureRecognizer();
        if (equationElement is not EquationGroup) {
            Label label = new Label {
                Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
            };
            string text;
            if (equationElement is UnknownElement) {
                UnknownElement unknownElement = equationElement as UnknownElement;
                bool isPositive = unknownElement.Number >= 0;
                bool isDifferentOfOne = unknownElement.Number != 1 && unknownElement.Number != -1;
                text = unknownElement.Symbol.ToString();
                Border multiplierSignBorder = new Border {
                    Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
                };
                Label multiplerSignLabel = new Label {
                    Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
                };
                multiplerSignLabel.Text = isPositive ? OperatorsSign[PossibleOperations.Sum] : OperatorsSign[PossibleOperations.Subtract];
                multiplierSignBorder.Content = multiplerSignLabel;
                multiplierSignBorder.IsVisible = !( isPositive && ( index == 0 || ( equationElements[index - 1] is EquationMultiplyOperator || equationElements[index - 1] is EquationDivideOperator ) ) ) ? true : false;
                yield return multiplierSignBorder;
                Border multiplierValueBorder = new Border {
                    Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
                };
                Label multiplierValueLabel = new Label {
                    Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
                };
                multiplierValueBorder.Margin = new Thickness(multiplierValueBorder.Margin.Left,multiplierValueBorder.Margin.Top,-16,multiplierValueBorder.Margin.Bottom);
                multiplierValueBorder.ZIndex = 10;
                multiplierValueBorder.Padding = new Thickness(multiplierValueBorder.Padding.Left,multiplierValueBorder.Padding.Top,0,multiplierValueBorder.Padding.Bottom);
                multiplierValueLabel.Text = unknownElement.Number.ToString().Replace("-","");
                multiplierValueBorder.Content = multiplierValueLabel;
                multiplierValueBorder.IsVisible = isDifferentOfOne;
                DragGestureRecognizer multiplierDragGestureRecognizer = new DragGestureRecognizer();
                yield return multiplierValueBorder;
                if (layout != null) {
                    multiplierDragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(isPositive ? ViewEquationElementType.Sum : ViewEquationElementType.Subtract,multiplierSignBorder,null));
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.UnknownElementMultiplierValue,multiplierValueBorder,unknownElement));
                        multiplierValueBorder.Margin = new Thickness(multiplierValueBorder.Margin.Left);
                        multiplierValueBorder.Padding = new Thickness(multiplierValueBorder.Padding.Left,multiplierValueBorder.Padding.Top,multiplierValueBorder.Padding.Left,multiplierValueBorder.Padding.Bottom);
                        DragGestureRecognizer_DragStarting(sender,e);
                    };
                    dragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                        if (equationElements.Count > 1) {
                            int numberOfNumberElementsBeforeThis = equationElements.ToArray()[..index].Count(element => element is NumberElement);
                            if (index != 0 && equationElements[index - 1] is IEquationOperator) {
                                IEquationOperator equationOperator = equationElements[index - 1] as IEquationOperator;
                                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) - 1) as Border,equationElements[index - 1]));
                            }
                            else if (index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator) {
                                IEquationOperator equationOperator = equationElements[index + 1] as IEquationOperator;
                                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) + 1) as Border,equationElements[index + 1]));
                            }
                        }
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.Subtract,multiplierSignBorder,null));
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.EquationElement,multiplierValueBorder,null));
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.EquationElement,border,equationElements[index]));
                        DragGestureRecognizer_DragStarting(sender,e);
                    };
                    multiplierDragGestureRecognizer.DropCompleted += async (object sender,DropCompletedEventArgs e) => {
                        await DragGestureRecognizer_DropCompleted(sender,e);
                        multiplierValueBorder.Margin = new Thickness(multiplierValueBorder.Margin.Left,multiplierValueBorder.Margin.Top,-16,multiplierValueBorder.Margin.Bottom);
                        multiplierValueBorder.ZIndex = 10;
                        multiplierValueBorder.Padding = new Thickness(multiplierValueBorder.Padding.Left,multiplierValueBorder.Padding.Top,0,multiplierValueBorder.Padding.Bottom);
                    };
                    dragGestureRecognizer.DropCompleted += (object sender,DropCompletedEventArgs e) => { DragGestureRecognizer_DropCompleted(sender,e); };
                    border.GestureRecognizers.Add(dragGestureRecognizer);
                    multiplierValueBorder.GestureRecognizers.Add(multiplierDragGestureRecognizer);
                }
            }
            else if (equationElement is NumberElement) {
                NumberElement numberElement = ( equationElement as NumberElement );
                bool isPositive = numberElement.Number >= 0;
                Border borderSign = new Border {
                    Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
                };
                Label labelSign = new Label {
                    Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style
                };
                labelSign.Text = isPositive ? OperatorsSign[PossibleOperations.Sum] : OperatorsSign[PossibleOperations.Subtract];
                borderSign.Content = labelSign;
                borderSign.IsVisible = !( isPositive && (index == 0 || (equationElements[index - 1] is EquationMultiplyOperator || equationElements[index - 1] is EquationDivideOperator ))) ? true : false;
                if (layout != null) {
                    bool hasBinaryOperator = equationElements.Count > 1 && ( ( index != 0 && equationElements[index - 1] is IEquationOperator ) || ( index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator ) );
                    dragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                        if (equationElements.Count > 1) {
                            var b = layout.Cast<Border>().Select(element => ( element.Content as Label ).Text);
                            int numberOfNumberElementsBeforeThis = equationElements.ToArray()[..( index + 1 )].Count(element => element is NumberElement);
                            if (index != 0 && equationElements[index - 1] is IEquationOperator) {
                                IEquationOperator equationOperator = equationElements[index - 1] as IEquationOperator;
                                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) - 1) as Border,equationElements[index - 1]));
                            }
                            else if (index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator) {
                                IEquationOperator equationOperator = equationElements[index + 1] as IEquationOperator;
                                ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) + 1) as Border,equationElements[index + 1]));
                            }
                        }
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(isPositive ? ViewEquationElementType.Sum : ViewEquationElementType.Subtract,borderSign,null));
                        ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.EquationElement,border,equationElements[index]));
                        DragGestureRecognizer_DragStarting(sender,e);
                    };
                    dragGestureRecognizer.DropCompleted += (object sender,DropCompletedEventArgs e) => { DragGestureRecognizer_DropCompleted(sender,e); };
                    border.GestureRecognizers.Add(dragGestureRecognizer);
                }
                yield return borderSign;
                text = numberElement.Number.ToString().Replace("-","");
            }
            else {
                border.Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style;
                text = equationElement is EquationMultiplyOperator ? OperatorsSign[PossibleOperations.Multiply] : OperatorsSign[PossibleOperations.Divide];
            }
            label.Text = text;
            border.Content = label;
        }
        else {
            HorizontalStackLayout elementsStackLayout = new HorizontalStackLayout {
                HorizontalOptions = LayoutOptions.Center
            };
            elementsStackLayout.Add(new Label {
                Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                Text = "("
            });
            EquationGroup equationGroup = equationElement as EquationGroup;
            foreach (EquationElement element in equationGroup.ElementsInGroup) {
                foreach (Border viewElement in GenerateViewEquationElement(equationGroup.ElementsInGroup,equationGroup.ElementsInGroup.IndexOf(element))) {
                    elementsStackLayout.Add(viewElement);
                }
            }
            elementsStackLayout.Add(new Label {
                Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                Text = ")"
            });
            border.Content = elementsStackLayout;
            if (layout != null) {
                dragGestureRecognizer.DragStarting += (object sender,DragStartingEventArgs e) => {
                    if (equationElements.Count > 1) {
                        int numberOfNumberElementsBeforeThis = equationElements.ToArray()[..index].Count(element => element is NumberElement);
                        if (index != 0 && equationElements[index - 1] is IEquationOperator) {
                            IEquationOperator equationOperator = equationElements[index - 1] as IEquationOperator;
                            ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) - 1) as Border,equationElements[index - 1]));
                        }
                        else if (index + 1 != equationElements.Count && equationElements[index + 1] is IEquationOperator) {
                            IEquationOperator equationOperator = equationElements[index + 1] as IEquationOperator;
                            ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(equationOperator is EquationMultiplyOperator ? ViewEquationElementType.Multiply : ViewEquationElementType.Divide,layout.ElementAt(( index + numberOfNumberElementsBeforeThis ) + 1) as Border,equationElements[index + 1]));
                        }
                    }
                    ElementsToDrag.Add(new Tuple<ViewEquationElementType,Border,EquationElement>(ViewEquationElementType.EquationElement,border,equationElements[index]));
                    DragGestureRecognizer_DragStarting(sender,e);
                };
                dragGestureRecognizer.DropCompleted += (object sender,DropCompletedEventArgs e) => { DragGestureRecognizer_DropCompleted(sender,e); };
                border.GestureRecognizers.Add(dragGestureRecognizer);
            }
        }

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

    private HorizontalStackLayout CloneEquationGroupViewContent(HorizontalStackLayout content) {
        HorizontalStackLayout elementsStackLayout = new HorizontalStackLayout {
            HorizontalOptions = LayoutOptions.Center
        };
        foreach (View element in content.Children) {
            if (element is Label) {
                Label label = element as Label;
                elementsStackLayout.Add(new Label {
                    Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                    Text = label.Text
                });
            }
            else {
                Border border = element as Border;
                Border borderClone = new Border {
                    Style = Resources.Single(resource => resource.Key == "defaultBorderStyle").Value as Style
                };
                if (border.Content is Label)
                    borderClone.Content = new Label {
                        Style = Resources.Single(resource => resource.Key == "defautlLabelStyle").Value as Style,
                        Text = ( border.Content as Label ).Text
                    };
                else
                    borderClone.Content = CloneEquationGroupViewContent(border.Content as HorizontalStackLayout);
                elementsStackLayout.Add(borderClone);
            }
        }
        return elementsStackLayout;
    }




}


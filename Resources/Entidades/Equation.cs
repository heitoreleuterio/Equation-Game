using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class Equation {
        public List<EquationElement> FirstSideElements { get; set; }
        public List<EquationElement> SecondSideElements { get; set; }

        public Equation(List<EquationElement> firstSideElements,List<EquationElement> secondeSideElements) {
            FirstSideElements = firstSideElements;
            SecondSideElements = secondeSideElements;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class NumberElement : EquationElement{
        public double Number { get; set; }
        public NumberElement(double number) {
            Number = number;
        }
    }
}

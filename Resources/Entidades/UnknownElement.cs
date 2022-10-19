using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class UnknownElement : EquationElement{
        public char Symbol { get; set; }
        
        public UnknownElement(char symbol) {
            Symbol = symbol;
        }
    }
}

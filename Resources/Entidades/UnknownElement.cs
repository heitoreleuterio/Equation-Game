using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class UnknownElement : NumberElement{
        public char Symbol { get; set; }
        
        public UnknownElement(char symbol,double number) : base(number) {
            Symbol = symbol;
        }

        public override bool Equals(object obj) {
            if(obj is UnknownElement) {
                UnknownElement unknownElement = obj as UnknownElement;
                return unknownElement.Symbol.Equals(Symbol) && unknownElement.Number.Equals(Number);
            }
            return false;
        }
    }
}

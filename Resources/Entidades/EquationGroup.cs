using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jogo_Algebra.Resources.Entidades {
    public class EquationGroup : EquationElement {
        public List<EquationElement> ElementsInGroup { get; set; }

        public EquationGroup(List<EquationElement> elementsInGroup) {
            ElementsInGroup = elementsInGroup;
        }

        public override bool Equals(object obj) {
            if(obj is EquationGroup) {
                EquationGroup equationGroup = obj as EquationGroup;
                return ElementsInGroup.SequenceEqual(equationGroup.ElementsInGroup);
            }
            return false;
        }
    }
}

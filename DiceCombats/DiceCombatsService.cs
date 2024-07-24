using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiceCombats
{
    public class DiceCombatsService
    {
        public List<DCCreature> CreatureList { get; set; } = new List<DCCreature>();

        public DCCreature? GetCreatureFromGUID(string guid)
        {
            return CreatureList.Find(x => x.Id.ToString() == guid);
        }
    }
}

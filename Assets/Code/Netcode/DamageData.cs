using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DamageData
{
    public int DamageDealt;
    public int AttackerViewId;
    public int VictimViewId;

    public object[] ToArray()
    {
        return new object[] { AttackerViewId, VictimViewId, DamageDealt };
    }
}

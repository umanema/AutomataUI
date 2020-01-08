using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VL.Lib.Collections;


namespace Automata.Enumerations
{
    #region TransitionsEnum
    public class TransitionsEnumDefinition : ManualDynamicEnumDefinitionBase<TransitionsEnumDefinition>
    {
        //this is optional an can be used if any initialization before the call to GetEntries is needed
        protected override void Initialize()
        {
        }

        //add this to get a node that can access the Instance from everywhere
        public static TransitionsEnumDefinition Instance => ManualDynamicEnumDefinitionBase<TransitionsEnumDefinition>.Instance;
    }

    [Serializable]
    public class TransitionsEnum : DynamicEnumBase<TransitionsEnum, TransitionsEnumDefinition>
    {
        public TransitionsEnum(string value) : base(value)
        {
        }

        public static void AddEntry(string entry)
        {
            if (!TransitionsEnumDefinition.Instance.ContainsEntry(entry))
            {
                TransitionsEnumDefinition.Instance.AddEntry(entry, null);
            }
        }

        public static void Clear()
        {
            TransitionsEnumDefinition.Instance.Clear();
        }

        public static TransitionsEnum CreateDefault()
        {
            return CreateDefaultBase();
        }
    }
    #endregion

    #region StatesEnum
    public class StatesEnumDefinition : ManualDynamicEnumDefinitionBase<StatesEnumDefinition>
    {
        //this is optional an can be used if any initialization before the call to GetEntries is needed
        protected override void Initialize()
        {
        }

        //add this to get a node that can access the Instance from everywhere
        public static StatesEnumDefinition Instance => ManualDynamicEnumDefinitionBase<StatesEnumDefinition>.Instance;
    }

    [Serializable]
    public class StatesEnum : DynamicEnumBase<StatesEnum, StatesEnumDefinition>
    {
        public StatesEnum(string value) : base(value)
        {
        }

        public static void AddEntry(string entry)
        {
            if (!StatesEnumDefinition.Instance.ContainsEntry(entry))
            {
                StatesEnumDefinition.Instance.AddEntry(entry, null);
            }
        }

        public static IReadOnlyList<string> GetEntries()
        {
            return StatesEnumDefinition.Instance.Entries;
        }

        public static void Clear()
        {
            StatesEnumDefinition.Instance.Clear();
        }

        public static StatesEnum CreateDefault()
        {
            return CreateDefaultBase();
        }
    }
    #endregion

    #region RegionsEnum
    public class RegionsEnumDefinition : ManualDynamicEnumDefinitionBase<RegionsEnumDefinition>
    {
        //this is optional an can be used if any initialization before the call to GetEntries is needed
        protected override void Initialize()
        {
        }

        //add this to get a node that can access the Instance from everywhere
        public static RegionsEnumDefinition Instance => ManualDynamicEnumDefinitionBase<RegionsEnumDefinition>.Instance;
    }

    [Serializable]
    public class RegionsEnum : DynamicEnumBase<RegionsEnum, RegionsEnumDefinition>
    {
        public RegionsEnum(string value) : base(value)
        {
        }

        public static void AddEntry(string entry)
        {
            if (!RegionsEnumDefinition.Instance.ContainsEntry(entry))
            {
                RegionsEnumDefinition.Instance.AddEntry(entry, null);
            }
        }

        public static void Clear()
        {
            RegionsEnumDefinition.Instance.Clear();
        }

        public static RegionsEnum CreateDefault()
        {
            return CreateDefaultBase();
        }
    }
    #endregion
}

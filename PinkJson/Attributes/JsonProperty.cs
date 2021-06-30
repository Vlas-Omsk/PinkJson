using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PinkJson
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class JsonProperty : Attribute
    {
        public string PropertyName { get; set; }
        public Type TargetType { get; set; }

        public JsonProperty()
        {
        }
        public JsonProperty(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
}

using System;

namespace PinkJson2
{
    public class ObjectsCountLessThanNecessary : PinkJsonException
    {
        public ObjectsCountLessThanNecessary() : base("The number of objects is less than necessary to return")
        {
        }
    }
}

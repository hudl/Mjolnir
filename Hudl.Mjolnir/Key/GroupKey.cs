using System;

namespace Hudl.Mjolnir.Key
{
    [Serializable]
    public struct GroupKey
    {
        private readonly string _name;

        public string Name
        {
            get { return _name; }
        }

        private GroupKey(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Create a group key with the provided name.
        /// 
        /// Good examples:
        ///
        /// - "mongo-highlights" (datasource-database)
        /// - "recruit-boards" (cluster-service)
        /// - "couchbase" (service/datasource)
        /// 
        /// </summary>
        public static GroupKey Named(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name cannot be null or empty", "name");
            }
            return new GroupKey(name);
        }

        public override string ToString()
        {
            return _name;
        }

        public override bool Equals(object obj)
        {
            return obj is GroupKey && this == (GroupKey) obj;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }

        public static bool operator ==(GroupKey x, GroupKey y)
        {
            return x._name == y._name;
        }

        public static bool operator !=(GroupKey x, GroupKey y)
        {
            return !(x == y);
        }
    }
}
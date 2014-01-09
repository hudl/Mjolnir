using System;
using System.Text.RegularExpressions;

namespace Hudl.Mjolnir.Key
{
    [Serializable]
    public struct GroupKey
    {
        private static readonly Regex KeyValidatorRegex = new Regex("^[A-Za-z][A-Za-z0-9_]{2,}$");

        private readonly string _name;

        public string Name
        {
            get { return _name; }
        }

        private GroupKey(string name)
        {
            if (!KeyValidatorRegex.IsMatch(name))
            {
                throw new ArgumentException("Key is invalid (use letters, numbers, underscores only)", "name");
            }
            _name = name;
        }

        /// <summary>
        /// Create a group key with the provided name.
        /// 
        /// Names must:
        /// - Be at least three characters long
        /// - Start with a letter
        /// - Contain only letters, numbers, and underscores
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GroupKey Named(string name)
        {
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
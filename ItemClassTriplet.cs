using System;

namespace ImprovedPublicTransport
{
    // IEquatable<T> + explicit Equals/GetHashCode: without them this struct falls back to
    // System.ValueType's default implementation, which uses reflection to compare every field.
    // Used as a Dictionary<ItemClassTriplet, HashSet<ushort>> key (BuildingExtension's depot map)
    // looked up from GetClosestDepot/StartTransfer - a genuinely hot path, not a one-off.
    public struct ItemClassTriplet : IEquatable<ItemClassTriplet>
    {

        public ItemClassTriplet(ItemClass.Service service, ItemClass.SubService subService, ItemClass.Level level)
        {
            this.Service = service;
            this.SubService = subService;
            this.Level = level;
        }


        public ItemClass.Service Service { get; }
        public ItemClass.SubService SubService { get; }
        public ItemClass.Level Level { get; }

        public bool IsValid()
        {
            return Service != ItemClass.Service.None;
        }

        public bool Equals(ItemClassTriplet other)
        {
            return Service == other.Service && SubService == other.SubService && Level == other.Level;
        }

        public override bool Equals(object obj)
        {
            return obj is ItemClassTriplet other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Service;
                hash = (hash * 397) ^ (int)SubService;
                hash = (hash * 397) ^ (int)Level;
                return hash;
            }
        }
    }
}
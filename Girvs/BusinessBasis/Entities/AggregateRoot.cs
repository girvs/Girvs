namespace Girvs.BusinessBasis.Entities
{
    public abstract class AggregateRoot<TPrimaryKey> : BaseEntity<TPrimaryKey>
    {
        public virtual void Merge(AggregateRoot<TPrimaryKey> obj)
        {
            var ps = this.GetType().GetProperties();
            foreach (var propertyInfo in ps)
            {
                var oldValue = propertyInfo.GetValue(this);
                var newValue = propertyInfo.GetValue(obj);
            }
        }
    }
}
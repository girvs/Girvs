namespace Girvs.Domain.Models
{
    public abstract class AggregateRoot : BaseEntity
    {
        public virtual void Merge(AggregateRoot obj)
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
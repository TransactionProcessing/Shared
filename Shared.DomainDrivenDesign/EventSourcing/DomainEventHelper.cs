namespace Shared.DomainDrivenDesign.EventSourcing;

using System;
using System.Linq;
using System.Reflection;

public static class DomainEventHelper{
    public static Boolean HasProperty(IDomainEvent domainEvent,
                                      String propertyName){
        PropertyInfo propertyInfo = domainEvent.GetType()
                                               .GetProperties()
                                               .SingleOrDefault(p => p.Name == propertyName);

        return propertyInfo != null;
    }

    public static T GetProperty<T>(IDomainEvent domainEvent, String propertyName, Boolean ignoreCase){
        PropertyInfo propertyInfo = null;
        PropertyInfo[] properties = domainEvent.GetType()
                                               .GetProperties();
        propertyInfo = ignoreCase ? properties.SingleOrDefault(p => String.Compare(p.Name, propertyName, StringComparison.CurrentCultureIgnoreCase) == 0) : properties.SingleOrDefault(p => p.Name == propertyName);

        if (propertyInfo != null){
            return (T)propertyInfo.GetValue(domainEvent);
        }

        return default(T);
    }

    public static T GetProperty<T>(IDomainEvent domainEvent,
                                   String propertyName) =>
        GetProperty<T>(domainEvent, propertyName, false);
}
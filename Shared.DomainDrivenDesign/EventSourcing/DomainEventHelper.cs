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

    public static T GetProperty<T>(IDomainEvent domainEvent, String propertyName, Boolean ignoreCase = false){
        PropertyInfo propertyInfo = null;
        PropertyInfo[] properties = domainEvent.GetType()
                                               .GetProperties();
        if (ignoreCase){
            propertyInfo = properties.SingleOrDefault(p => String.Compare(p.Name, propertyName, StringComparison.CurrentCultureIgnoreCase) == 0);
        }
        else{
            propertyInfo = properties.SingleOrDefault(p => p.Name == propertyName);
        }

        if (propertyInfo != null){
            return (T)propertyInfo.GetValue(domainEvent);
        }

        return default(T);
    }
}
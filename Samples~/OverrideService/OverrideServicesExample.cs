using System;
using System.Collections.Generic;
using SharedServices.Locator.V1;
using SharedServices.V1;

namespace Samples.SharedServices.Locator.V1.Override_Service
{
    public class OverrideServicesExample : IOverrideServices
    {
        public void OverrideServices(Dictionary<Type, IService> services)
        {
            // Service Locator will use the following concrete services instead of the default ones
            // Additionally, these services will be initialized in the order they are added here
            
            // services[typeof(IEventService)] = new EventService();
            // services[typeof(ITextEditorService)] = new TextEditorService();
            // services[typeof(IAnalyticsService)] = new AnalyticsService();
        }
    }
}
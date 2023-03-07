namespace Facepunch.Forsaken.FlowFields.Algorithms
{
    public class IntegrationsPathService : IntegrationService
    {
        private static IntegrationsPathService _default;

        public static IntegrationsPathService Default
        {
            get
            {
				_default ??= new();
				return _default;
            }
        }
    }
}

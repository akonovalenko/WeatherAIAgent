namespace WeatherAIAgent.Helpers
{
    /// <summary>
    /// Helper methods for the AgentService class.
    /// </summary>
    public static class AgentServiceHelpers
    {

        /// <summary>
        /// Tries to get an integer value from the specified object by checking the provided property names in order. If a property is found and its value can be converted to an integer, that value is returned. If no valid integer value is found, 0 is returned.
        /// </summary>
        /// <param name="usageObj">The object to check for the specified properties.</param>
        /// <param name="names">The property names to check in order.</param>
        /// <returns>The integer value if found, otherwise 0.</returns>
        public static int TryGetInt(object obj, params string[] names)
        {
            foreach (var name in names)
            {
                var value = obj.GetType().GetProperty(name)?.GetValue(obj);
                if (value == null) continue;

                return value switch
                {
                    int i => i,
                    long l => (int)l,
                    double d => (int)d,
                    _ => int.TryParse(value.ToString(), out var i) ? i : 0
                };
            }

            return 0;
        }
    }
}
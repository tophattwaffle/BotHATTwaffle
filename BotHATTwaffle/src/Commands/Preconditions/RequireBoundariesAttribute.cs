using System;
using System.Threading.Tasks;

using Discord.Commands;

namespace BotHATTwaffle.Commands.Preconditions
{
    /// <summary>
    /// This attribute requires that the command parameter's value is within an inclusive range of values.
    /// A low, upper, or both boundaries can be specified.
    /// Only supports primitive types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class RequireBoundariesAttribute : ParameterPreconditionAttribute
    {
        private object _lower;
        private object _upper;

        public RequireBoundariesAttribute() { }

        public RequireBoundariesAttribute(object lower, object upper)
        {
            Lower = lower;
            Upper = upper;
        }

        /// <summary>
        /// The inclusive lower boundary constraint.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when set to a non-null, non-primitive value.</exception>
        public object Lower
        {
            get => _lower;
            set
            {
                if (value != null && !value.GetType().IsPrimitive)
                    throw new ArgumentException("Boundary value must be of a primitive type.", nameof(value));

                _lower = value;
            }
        }

        /// <summary>
        /// The inclusive upper boundary constraint.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when set to a non-null, non-primitive value.</exception>
        public object Upper
        {
            get => _upper;
            set
            {
                if (value != null && !value.GetType().IsPrimitive)
                    throw new ArgumentException("Boundary value must be of a primitive type.", nameof(value));

                _upper = value;
            }
        }

        public override Task<PreconditionResult> CheckPermissions(
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services)
        {
            Type type = Nullable.GetUnderlyingType(parameter.Type) ?? parameter.Type;

            if (!type.IsPrimitive)
                throw new ArgumentException($"Parameter {parameter.Name} must be of a primitive type.", parameter.Name);

            dynamic val = Convert.ChangeType(value, type);

            if (_lower != null && val.CompareTo(_lower) < 0)
            {
                return Task.FromResult(
                    PreconditionResult.FromError(
                        $"Parameter `{parameter.Name}` is below the lower boundary constraint of `{_lower}`."));
            }

            if (_upper != null && val.CompareTo(_upper) > 0)
            {
                return Task.FromResult(
                    PreconditionResult.FromError(
                        $"Parameter `{parameter.Name}` is above the upper boundary constraint of `{_upper}`."));
            }

            return Task.FromResult(PreconditionResult.FromSuccess());
        }
    }
}

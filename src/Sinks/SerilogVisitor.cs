namespace Serilog.Sinks.Fluentd.Core.Sinks
{
    using System;
    using System.Collections.Generic;
    using Serilog.Events;

    public class SerilogVisitor
    {
        public void Visit(IDictionary<string, object> state, string name, LogEventPropertyValue value)
        {
            var returnValue = this.Visit(value);
            state[name] = returnValue;
        }

        private object Visit(LogEventPropertyValue value)
        {
            switch (value)
            {
                case ScalarValue scalar:
                    return this.VisitScalarValue(scalar);
                case SequenceValue sequence:
                    return this.VisitSequenceValue(sequence);
                case StructureValue structure:
                    return this.VisitStructureValue(structure);
                case DictionaryValue dictionary:
                    return this.VisitDictionaryValue(dictionary);
                default:
                    throw new NotSupportedException(string.Format("The value {0} is not of a type supported by this visitor.", value));
            }
        }

        private object VisitScalarValue(ScalarValue scalar)
        {
            if (scalar.Value is Guid)
            {
                return scalar.Value.ToString();
            }
            
            return scalar.Value;
        }

        private IEnumerable<object> VisitSequenceValue(SequenceValue sequence)
        {
            var list = new List<object>();

            foreach (var sequenceElement in sequence.Elements)
            {
                list.Add(this.Visit(sequenceElement));
            }

            return list;
        }

        private IDictionary<string, object> VisitStructureValue(StructureValue structure)
        {
            var dic = new Dictionary<string, object>();

            foreach (var structureProperty in structure.Properties)
            {
                dic[structureProperty.Name] = this.Visit(structureProperty.Value);
            }

            return dic;
        }

        private IDictionary<string, object> VisitDictionaryValue(DictionaryValue dictionary)
        {
            var dic = new Dictionary<string, object>();

            foreach (var element in dictionary.Elements)
            {
                dic[element.Key.Value.ToString()] = this.Visit(element.Value);
            }

            return dic;
        }
    }
}

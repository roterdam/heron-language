using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HeronEngine
{
    public class RecordLayout
    {
        List<string> names;
        List<HeronType> types;

        public RecordLayout(List<string> names, List<HeronType> types)
        {
            this.names = names;
            this.types = types;

            for (int i=0; i < names.Count; ++i)
            {
                if (!names[i].IsValidIdentifier())
                    throw new Exception("Field names must be valid identifiers");

                for (int j = i + 1; j < names.Count; ++j)
                {
                    if (names[j] == names[i])
                        throw new Exception("Fields names must not be repeated");
                }
            }

            if (types.Count != names.Count)
                throw new Exception("Not a valid record layout: all types must have an associated name (even if empty)");
            if (types.Count == 0)
                throw new Exception("Not a valid record layout: at least one type is required");
        }

        public HeronType GetIndexType()
        {
            return types[0];
        }

        public int Count
        {
            get
            {
                return names.Count;
            }
        }

        public List<string> GetNames()
        {
            return names;
        }

        public List<HeronType> GetTypes()
        {
            return types;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < types.Count; ++i)
            {
                sb.Append(names[i]);
                sb.Append("=");
                sb.Append(types[i].ToString());
                sb.Append(";");
            }
            return sb.ToString();
        }

        public bool HasName(string s)
        {
            return names.Contains(s);
        }

        public int GetFieldIndex(string s)
        {
            int n = names.IndexOf(s);
            return n;
        }

        public HeronType GetTypeOf(string s)
        {
            int n = names.IndexOf(s);
            return types[n];
        }

        public bool IsCompatible(RecordValue x)
        {
            for (int i = 0; i < names.Count; ++i)
            {
                string name = names[i];
                HeronType type = types[i];
                int n = x.GetFieldIndex(name);
                if (n < 0)
                    return false;
                HeronValue val = x.GetValue(n);
                HeronValue test = val.As(type);
                if (test == null)
                    return false;
            }
            return true;
        }

        public bool IsCompatible(ListValue x)
        {
            if (x.InternalCount() != Count)
                return false;
            for (int i = 0; i < Count; ++i)
            {
                HeronValue val = x.InternalGetAtIndex(i);
                HeronType type = GetTypes()[i];
                HeronValue test = val.As(type);
                if (test == null)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// A record is ta dictionary that can't add and remove values. 
    /// </summary>
    public class RecordValue : SeqValue
    {
        RecordLayout layout;
        List<HeronValue> values;        

        public RecordValue(RecordLayout layout, List<HeronValue> values)
        {
            if (layout == null)
                throw new ArgumentNullException("missing layout value");
            if (values.Count != layout.Count)
                throw new Exception("Layout has different number of values from record");
            this.layout = layout;
            this.values = values;
        }

        [HeronVisible]
        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.RecordType;
        }

        public override IteratorValue GetIterator()
        {
            return new ListToIterValue(values);
        }

        public HeronValue GetValue(int n)
        {
            return values[n];
        }

        public void SetValue(int n, HeronValue val)
        {
            values[n] = val;
        }

        [HeronVisible]
        public bool HasKey(string s)
        {
            return GetFieldIndex(s) >= 0;
        }

        [HeronVisible]
        public int GetFieldIndex(string s)
        {
            return layout.GetFieldIndex(s);
        }

         public void SetValue(string s, HeronValue val)
        {
            int n = GetFieldIndex(s);
            if (n < 0)
                throw new Exception("record does not have field: " + s);
            values[n] = val;
        }

        public HeronValue GetValue(string s)
        {
            int n = GetFieldIndex(s);
            if (n < 0)
                throw new Exception("record does not have field: " + s);
            return values[n];
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            if (index is StringValue)
            {
                return GetValue((index as StringValue).GetValue());
            }
            else if (index is IntValue)
            {
                return GetValue((index as IntValue).GetValue());
            }
            else
            {
                throw new Exception("Can only index records using strings or integers");
            }
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            if (index is StringValue)
            {
                SetValue((index as StringValue).GetValue(), val);
            }
            else if (index is IntValue)
            {
                SetValue((index as IntValue).GetValue(), val);
            }
            else
            {
                throw new Exception("Can only index records using strings or integers");
            }
        }

        public RecordLayout GetLayout()
        {
            return layout;
        }

        [HeronVisible]
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{ ");
            for (int i = 0; i < values.Count; ++i)
            {
                if (i > 0) sb.Append(",");
                sb.Append(layout.GetNames()[i]);
                sb.Append("=");
                sb.Append(values[i].ToString());
            }
            sb.Append("}");
            return sb.ToString();
        }

        public override HeronValue GetFieldOrMethod(string name)
        {
            if (HasKey(name))
                return GetValue(name);
            return base.GetFieldOrMethod(name);
        }

        public override HeronValue[] ToArray()
        {
            return values.ToArray();
        }
    }

    /// <summary>
    /// A record is ta dictionary that can't add and remove values. 
    /// </summary>
    public class TableValue : SeqValue
    {
        Dictionary<int, RecordValue> values = new Dictionary<int, RecordValue>();
        RecordLayout layout;

        public TableValue(RecordLayout layout)
        {
            if (layout.Count < 2)
                throw new Exception("A table must consist of at least two fields");
            this.layout = layout;
        }

        [HeronVisible]
        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TableType;
        }

        public override IteratorValue GetIterator()
        {
            return new ListToIterValue(values.Values);
        }

        public string GetIndexField()
        {
            return layout.GetNames()[0];
        }

        public HeronValue GetIndexValue(RecordValue rec)
        {
            return rec.GetValue(GetIndexField());
        }

        [HeronVisible]
        public void Add(HeronValue record)
        {
            CheckRecordCompatibility(record);
            if (record is RecordValue)
            {
                RecordValue rec = record as RecordValue;
                int n = GetIndexValue(rec).GetHashCode();
                values.Add(n, rec);
            }
            else 
            {
                ListValue list = record as ListValue;
                if (list == null)
                    throw new Exception("Can only add lists or records to a table");
                RecordValue rec = new RecordValue(layout, list.InternalList());
                int n = GetIndexValue(rec).GetHashCode();
                values.Add(n, rec);
            }
        }

        [HeronVisible]
        public void Remove(HeronValue index)
        {
            CheckIndexType(index);
            int n = index.GetHashCode();
            values.Remove(n);
        }

        public override HeronValue GetAtIndex(HeronValue index)
        {
            CheckIndexType(index);
            return values[index.GetHashCode()];
        }

        public override void SetAtIndex(HeronValue index, HeronValue val)
        {
            CheckRecordCompatibility(val);
            CheckIndexType(index);
            values[index.GetHashCode()] = val as RecordValue;
        }

        private void CheckRecordCompatibility(HeronValue record)
        {
            if (!(record is RecordValue))
            {
                if (!(record is ListValue))
                    throw new Exception(record.ToString() + " is not a valid list or record type");
                ListValue list = record as ListValue;
                if (layout.IsCompatible(list))
                    return;
                throw new Exception("The list value is not compatible");
            }
            else
            {
                RecordValue rec = record as RecordValue;
                if (rec.GetLayout() == layout)
                    return;
                // Check then that there are the same fields in "record" that we require. 
                if (layout.IsCompatible(rec))
                    return;
                throw new Exception("The record layout " + layout.ToString() + " does not contain a super-set of the accepted fields");
            }
        }

        private void CheckIndexType(HeronValue index)
        {
            if (index.As(layout.GetIndexType()) == null)
                throw new Exception(index.ToString() + " is not a valid index type, expected " + layout.GetIndexType().ToString());
        }

        [HeronVisible]
        public bool HasKey(HeronValue index)
        {
            CheckIndexType(index);
            return values.ContainsKey(index.GetHashCode());
        }

        public override HeronValue[] ToArray()
        {
            return values.Values.ToArray();
        }
    }
}

/// Heron language interpreter for Windows in C#
/// http://www.heron-language.com
/// Copyright (c) 2009 Christopher Diggins
/// Licenced under the MIT License 1.0 
/// http://www.opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEngine
{
    /// <summary>
    /// NOT CURRENTLY USED!
    /// </summary>
    public class TreeNodeValue : HeronValue 
    {
        List<TreeNodeValue> nodes = new List<TreeNodeValue>();
        HeronValue data;

        TreeNodeValue(HeronValue val)
        {
            data = val;
        }

        [HeronVisible]
        IntValue Count()
        {
            return new IntValue(nodes.Count);
        }

        [HeronVisible]
        TreeNodeValue Child(IntValue n)
        {
            return nodes[n.GetValue()];
        }

        [HeronVisible]
        public SeqValue Children()
        {
            return new EnumeratorToHeronAdapter(ChildrenIterator().GetEnumerator());
        }

        [HeronVisible]
        public HeronValue Data()
        {
            return data;
        }

        [HeronVisible]
        public void Swap(TreeNodeValue node1, TreeNodeValue node2)
        {
            throw new NotImplementedException();
        }

        [HeronVisible]
        public void SetData(HeronValue val)
        {
            data = val;
        }

        [HeronVisible]
        public void Add(HeronValue val)
        {
            new TreeNodeValue(val);
        }

        [HeronVisible]
        public void Remove()
        {
            nodes.RemoveAt(nodes.Count - 1);
        }

        [HeronVisible]
        public SeqValue Descendants()
        {
            return new EnumeratorToHeronAdapter(DescendantsIterator().GetEnumerator());
        }

        #region private functions
        private IEnumerable<HeronValue> DescendantsIterator()
        {
            foreach (TreeNodeValue child in nodes)
                foreach (HeronValue tmp in child.DescendantsIterator())
                    yield return tmp;
            yield return this;
        }
        private IEnumerable<HeronValue> ChildrenIterator()
        {
            foreach (TreeNodeValue child in nodes)
                yield return child;
        }
        #endregion

        public override HeronType GetHeronType()
        {
            return PrimitiveTypes.TreeType;
        }
    }
}

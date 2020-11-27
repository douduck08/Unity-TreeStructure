using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DouduckLib {
    public class BinaryTreeNode<T> where T : class {
        public T value { get; set; }
        public BinaryTreeNode (T value) {
            this.value = value;
            m_parent = null;
            m_children = new TreeNodeList (this);
        }

        private BinaryTreeNode<T> m_parent;
        public BinaryTreeNode<T> parent {
            get { return m_parent; }
            protected set {
                if (value == m_parent) {
                    return;
                }
                if (m_parent != null) {
                    m_parent.children.Remove(this);
                }
                if (value != null && !value.children.Contains(this)) {
                    throw new System.InvalidOperationException ("Cannot directly set parent, use children setter");
                }
                m_parent = value;
            }
        }

        public BinaryTreeNode<T> Root {
            get {
                BinaryTreeNode<T> node = this;
                while (node.parent != null) {
                    node = node.parent;
                }
                return node;
            }
        }

        private TreeNodeList m_children;
        public TreeNodeList children {
            get { return m_children; }
        }
        public BinaryTreeNode<T> left {
            get { return m_children[0]; }
        }
        public BinaryTreeNode<T> right {
            get { return m_children[1]; }
        }

        public class TreeNodeList {
            public BinaryTreeNode<T> parent;
            private List<BinaryTreeNode<T>> list;

            public BinaryTreeNode<T> this[int i] {
                get { return list[i]; }
                set {
                    list[i] = value;
                    value.parent = parent;
                }
            }

            public TreeNodeList(BinaryTreeNode<T> parent) {
                this.parent = parent;
                this.list = new List<BinaryTreeNode<T>> ();
                list.Add (null);
                list.Add (null);
            }

            public bool Contains (BinaryTreeNode<T> node) {
                return list.Contains (node);
            }

            public void Remove (BinaryTreeNode<T> node) {
                int index = list.FindIndex (p => p == node);
                if (index != -1) {
                    list[index] = null;
                }
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System;

namespace DouduckLib {
    public class TreeNode<T> where T : class {
        public T value { get; set; }
        public TreeNode (T value) {
            this.value = value;
            m_parent = null;
            m_children = new TreeNodeList (this);
        }

        private TreeNode<T> m_parent;
        public TreeNode<T> parent {
            get { return m_parent; }
            set {
                if (value == m_parent) {
                    return;
                }
                if (m_parent != null) {
                    m_parent.children.Remove(this);
                }
                if (value != null && !value.children.Contains(this)) {
                    value.children.Add(this);
                }
                m_parent = value;
            }
        }

        public TreeNode<T> Root {
            get {
                TreeNode<T> node = this;
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

        public class TreeNodeList : List<TreeNode<T>> {
            public TreeNode<T> parent;
            public TreeNodeList(TreeNode<T> parent) {
                this.parent = parent;
            }

            public new TreeNode<T> Add(TreeNode<T> node) {
                base.Add(node);
                node.parent = parent;
                return node;
            }

            public TreeNode<T> Add(T value) {
                return Add(new TreeNode<T>(value));
            }

            public override string ToString() {
                return "Count = " + Count.ToString();
            }
        }
    }
}
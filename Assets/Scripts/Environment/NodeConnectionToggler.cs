using UnityEngine;

namespace RW.MonumentValley
{
    /// <summary>
    /// A helper script to easily disconnect and reconnect a path between two nodes using UnityEvents (e.g. from buttons or switches).
    /// </summary>
    public class NodeConnectionToggler : MonoBehaviour
    {
        [Tooltip("The first node in the path connection.")]
        public Node nodeA;

        [Tooltip("The second node in the path connection.")]
        public Node nodeB;

        [Tooltip("If false, the path between these nodes will be disconnected immediately when the game starts.")]
        public bool startConnected = true;

        private void Start()
        {
            if (!startConnected)
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (nodeA != null && nodeB != null)
            {
                // We disconnect BOTH directions to ensure the cat can't walk either way!
                nodeA.EnableEdge(nodeB, false);
                nodeB.EnableEdge(nodeA, false);
                Debug.Log($"[NodeConnectionToggler] Disconnected {nodeA.name} and {nodeB.name}");
            }
        }

        public void Reconnect()
        {
            if (nodeA != null && nodeB != null)
            {
                // We reconnect BOTH directions
                nodeA.EnableEdge(nodeB, true);
                nodeB.EnableEdge(nodeA, true);
                Debug.Log($"[NodeConnectionToggler] Reconnected {nodeA.name} and {nodeB.name}");
            }
        }

        public void ToggleConnection()
        {
            if (nodeA != null && nodeB != null)
            {
                // Check if they are currently connected
                bool isConnected = nodeA.IsEdgeActive(nodeB);
                
                if (isConnected) Disconnect();
                else Reconnect();
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Utilities
{
    /// <summary>
    /// Provee funcionalidad de búsqueda de ruta en una grilla ortogonal.
    /// </summary>
    public static class GridPathfinder
    {
        /// <summary>
        /// Direcciones permitidas (solo horizontal y vertical).
        /// </summary>
        public static readonly Vector2Int[] Directions = new Vector2Int[]
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        /// <summary>
        /// Encuentra una ruta desde start hasta goal usando BFS en una grilla.
        /// No considera colisiones explícitas: se asume que la física manejará los bloqueos.
        /// </summary>
        /// <param name="start">Posición de inicio en coordenadas de celda.</param>
        /// <param name="goal">Posición objetivo en coordenadas de celda.</param>
        /// <returns>Lista de celdas a recorrer, excluyendo la celda de inicio.</returns>
        public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
        {
            var frontier = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

            frontier.Enqueue(start);
            cameFrom[start] = start;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                if (current == goal)
                    break;

                foreach (var dir in Directions)
                {
                    var next = current + dir;
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            // Reconstrucción de la ruta
            var path = new List<Vector2Int>();
            if (!cameFrom.ContainsKey(goal))
                return path;

            var step = goal;
            while (step != start)
            {
                path.Insert(0, step);
                step = cameFrom[step];
            }
            return path;
        }
    }

    /// <summary>
    /// Controlador de movimiento en grilla para un GameObject con Rigidbody2D.
    /// Utiliza GridPathfinder para obtener el camino hacia un objetivo.
    /// </summary>
    public class GridMover : MonoBehaviour
    {
        [Header("Configuración de movimiento")]
        [SerializeField] private float moveSpeed = 2f;
        [Tooltip("Transform del objetivo hacia el que el objeto se moverá")]
        [SerializeField] private Transform target;
        [Tooltip("Componente para actualizar sprites según la dirección de movimiento")]
        [SerializeField] private DirectionSpriteController directionController;

        private Rigidbody2D rb;
        private List<Vector2Int> path;
        private int pathIndex;
        private Vector2 gridSize = Vector2.one;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
                Debug.LogError("Rigidbody2D no encontrado en " + name);
        }

        void Start()
        {
            if (directionController == null)
                directionController = GetComponent<DirectionSpriteController>();
        }

        void Update()
        {
            if (target == null)
                return;

            Vector2Int currentCell = WorldToGrid(transform.position);
            Vector2Int targetCell = WorldToGrid(target.position);

            // Recalcular la ruta si es necesario
            if (path == null || pathIndex >= path.Count)
            {
                path = GridPathfinder.FindPath(currentCell, targetCell);
                pathIndex = 0;
            }

            if (path.Count > 0)
                MoveAlongPath();
        }

        private void MoveAlongPath()
        {
            Vector2Int nextCell = path[pathIndex];
            Vector2 nextWorldPos = GridToWorld(nextCell);
            Vector2 direction = (nextWorldPos - (Vector2)transform.position).normalized;

            // Actualizar sprite
            directionController?.UpdateDirection(direction);

            // Movimiento usando Rigidbody2D
            Vector2 movement = direction * moveSpeed * Time.deltaTime;
            if (Vector2.Distance(transform.position, nextWorldPos) <= movement.magnitude)
            {
                rb.MovePosition(nextWorldPos);
                pathIndex++;
            }
            else
            {
                rb.MovePosition(rb.position + movement);
            }
        }

        private Vector2Int WorldToGrid(Vector2 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / gridSize.x),
                Mathf.RoundToInt(worldPosition.y / gridSize.y)
            );
        }

        private Vector2 GridToWorld(Vector2Int gridPosition)
        {
            return new Vector2(
                gridPosition.x * gridSize.x,
                gridPosition.y * gridSize.y
            );
        }
    }
}

# Spatial

Namespace: `DataKeeper.Spatial`

`Octree<T>` (3D) and `Quadtree<T>` (2D) partition registered components spatially for fast proximity and bounds queries. `T` is any `Component`; positions come from `transform.position`.

## Quick start

```csharp
var tree = new Octree<Enemy>(maxDepth: 5, maxComponentsPerNode: 10);

tree.AddComponents(FindObjectsByType<Enemy>(FindObjectsSortMode.None));
tree.BuildTree(); // bakes current positions into the tree

List<Enemy> nearby = tree.GetComponentsInRadius(player.position, 25f);
List<Enemy> inZone = tree.GetComponentsInBounds(zoneBounds);
```

`Quadtree<T>` is identical but 2D: `GetComponentsInRadius(Vector2, float)` and `GetComponentsInBounds(Rect)`.

## API

| Member | Description |
| --- | --- |
| `AddComponent` / `AddComponents` / `RemoveComponent` / `Clear` | Manage the registered set |
| `BuildTree()` | (Re)build the tree from current component positions; root bounds are computed automatically |
| `GetComponentsInRadius(pos, radius)` | Components within a sphere/circle |
| `GetComponentsInBounds(bounds)` | Components within a `Bounds`/`Rect` |
| `AllComponents` / `Root` | Raw access for custom traversal |
| `DrawGizmos()` | Draw node bounds from `OnDrawGizmos` for debugging |

## Notes

- The tree is a **snapshot**: it indexes positions at `BuildTree()` time. For moving objects, rebuild at whatever cadence your query accuracy requires (e.g. a few times per second) — adding/removing components alone does not update the spatial index.
- Inactive or destroyed components are skipped during the build.
- Tune `maxDepth` / `maxComponentsPerNode` to your object counts: deeper trees are faster to query but slower to build.

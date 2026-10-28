using UnityEngine;

namespace Framework.ECS
{
    [DefaultExecutionOrder(-100)]
    public sealed class EcsRuntime : UnitySingleton<EcsRuntime>
    {
        public EcsWorld World { get; private set; }
        public void Init()
        {
            if (World != null && !World.IsDisposed) return;
            World = new EcsWorld();
            World.ExceptionHandler = exception => Debug.LogException(exception);
        }

        private void Update() { if (World != null && !World.IsDisposed) World.Update(Time.deltaTime); }
        private void LateUpdate() { if (World != null && !World.IsDisposed) World.LateUpdate(); }
        private void OnDestroy() { World?.Dispose(); }
    }
}

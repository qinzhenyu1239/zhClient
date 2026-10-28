using System;
using System.Collections.Generic;
using System.Reflection;

namespace Framework.ECS
{
    public sealed class EcsWorld : IDisposable
    {
        private readonly List<Entity> updates = new List<Entity>();
        private readonly List<Entity> lateUpdates = new List<Entity>();
        private readonly List<Entity> pendingUpdates = new List<Entity>();
        private readonly List<Entity> pendingLateUpdates = new List<Entity>();
        private bool dispatching;
        private long nextId;
        public Entity Root { get; }
        public EntitySystemRegistry Systems { get; }
        public double DeltaTime { get; private set; }
        public bool IsDisposed { get; private set; }
        public Action<Exception> ExceptionHandler { get; set; }

        public EcsWorld() : this(EntitySystemRegistry.Scan(typeof(Entity).Assembly)) { }
        public EcsWorld(EntitySystemRegistry systems)
        {
            Systems = systems ?? throw new ArgumentNullException(nameof(systems));
            Root = new Entity();
            Root.Attach(this, null);
        }

        internal long AllocateId() => ++nextId;

        internal void Register(Entity entity)
        {
            if (entity is IUpdate && Systems.Has(entity, "Update"))
                (dispatching ? pendingUpdates : updates).Add(entity);
            if (entity is ILateUpdate && Systems.Has(entity, "LateUpdate"))
                (dispatching ? pendingLateUpdates : lateUpdates).Add(entity);
        }

        public void Update(double deltaTime)
        {
            EnsureLive();
            if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            DeltaTime = deltaTime;
            updates.AddRange(pendingUpdates);
            pendingUpdates.Clear();
            lateUpdates.AddRange(pendingLateUpdates);
            pendingLateUpdates.Clear();
            Dispatch(updates, "Update");
        }

        public void LateUpdate()
        {
            EnsureLive();
            Dispatch(lateUpdates, "LateUpdate");
        }

        private void EnsureLive()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(EcsWorld));
            if (dispatching) throw new InvalidOperationException("Recursive ECS update is not supported.");
        }

        private void Dispatch(List<Entity> queue, string lifecycle)
        {
            dispatching = true;
            try
            {
                int count = queue.Count;
                int retained = 0;
                for (int i = 0; i < count; i++)
                {
                    Entity entity = queue[i];
                    if (entity.IsDisposed) continue;
                    queue[retained++] = entity;
                    if (!entity.IsActive) continue;
                    try { Systems.Invoke(entity, lifecycle); }
                    catch (Exception exception)
                    {
                        Report(new InvalidOperationException("ECS " + lifecycle + " failed: " + entity.GetType().FullName, exception));
                    }
                }
                if (retained < count) queue.RemoveRange(retained, count - retained);
            }
            finally
            {
                dispatching = false;
                if (IsDisposed) { updates.Clear(); lateUpdates.Clear(); }
            }
        }

        internal void Report(Exception exception)
        {
            try
            {
                if (ExceptionHandler != null) ExceptionHandler(exception);
                else System.Diagnostics.Trace.TraceError(exception.ToString());
            }
            catch { System.Diagnostics.Trace.TraceError(exception.ToString()); }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            Root.Dispose();
            IsDisposed = true;
            if (!dispatching) { updates.Clear(); lateUpdates.Clear(); }
            pendingUpdates.Clear();
            pendingLateUpdates.Clear();
        }
    }
}

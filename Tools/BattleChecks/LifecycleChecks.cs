using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Framework.ECS;

public sealed class LifecycleHost : Entity { }

[ComponentOf(typeof(LifecycleHost))]
public sealed class LifecycleProbe : Entity, IAwake, IUpdate, ILateUpdate, IDestroy
{
    public int Awakes;
    public int Updates;
    public int LateUpdates;
    public int Destroys;
    public bool ParentAvailableOnDestroy;
    public Action<LifecycleProbe> DuringUpdate;
}

[EntitySystemOf(typeof(LifecycleProbe))]
public static class LifecycleProbeSystem
{
    [EntitySystem] private static void Awake(this LifecycleProbe self) { self.Awakes++; }
    [EntitySystem] private static void Update(this LifecycleProbe self) { self.Updates++; self.DuringUpdate?.Invoke(self); }
    [EntitySystem] private static void LateUpdate(this LifecycleProbe self) { self.LateUpdates++; }
    [EntitySystem] private static void Destroy(this LifecycleProbe self)
    {
        self.Destroys++;
        self.ParentAvailableOnDestroy = self.GetParent<LifecycleHost>() != null;
    }
}

[ComponentOf(typeof(LifecycleHost))]
public sealed class FailingAwakeProbe : Entity, IAwake, IDestroy
{
    public static int Destroyed;
}

[EntitySystemOf(typeof(FailingAwakeProbe))]
public static class FailingAwakeProbeSystem
{
    [EntitySystem] private static void Awake(this FailingAwakeProbe self) { throw new InvalidOperationException("awake failure"); }
    [EntitySystem] private static void Destroy(this FailingAwakeProbe self) { FailingAwakeProbe.Destroyed++; }
}

static class LifecycleChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception(message);
    }

    public static void Run()
    {
        using var world = new EcsWorld();
        LifecycleHost host = world.Root.AddChild<LifecycleHost>();
        LifecycleProbe probe = host.AddComponent<LifecycleProbe>();
        Check(probe.Awakes == 1 && probe.Parent == host && host.GetComponent<LifecycleProbe>() == probe, "Automatic Awake and parent binding");
        Throws<InvalidOperationException>(() => host.AddComponent<LifecycleProbe>(), "Reject duplicate component");
        Throws<InvalidOperationException>(() => world.Root.AddComponent<LifecycleProbe>(), "Enforce component parent type");
        world.Update(0.1);
        world.LateUpdate();
        Check(probe.Updates == 1 && probe.LateUpdates == 1, "Automatic Update and LateUpdate dispatch");
        host.Enabled = false;
        world.Update(0.1);
        world.LateUpdate();
        Check(probe.Updates == 1 && probe.LateUpdates == 1, "Hierarchy pause");
        host.Enabled = true;
        host.RemoveComponent<LifecycleProbe>();
        Check(probe.IsDisposed && probe.Destroys == 1 && probe.ParentAvailableOnDestroy, "Destroy runs once with parent available");
        LifecycleProbe replacement = host.AddComponent<LifecycleProbe>();
        world.Update(0.1);
        Check(probe.Updates == 1 && replacement.Updates == 1, "Removed component leaves lifecycle queue");

        LifecycleProbe added = null;
        replacement.DuringUpdate = self =>
        {
            self.DuringUpdate = null;
            added = host.AddChild<LifecycleHost>().AddComponent<LifecycleProbe>();
        };
        world.Update(0.1);
        world.LateUpdate();
        Check(added.Awakes == 1 && added.Updates == 0 && added.LateUpdates == 0, "New component starts updating on next frame");
        world.Update(0.1);
        world.LateUpdate();
        Check(added.Updates == 1 && added.LateUpdates == 1, "Deferred component automatically enters queues");

        var errors = new List<Exception>();
        world.ExceptionHandler = errors.Add;
        replacement.DuringUpdate = _ => throw new InvalidOperationException("update failure");
        int previous = added.Updates;
        world.Update(0.1);
        Check(errors.Count == 1 && errors[0].Message.Contains(nameof(LifecycleProbe)) && added.Updates == previous + 1,
            "Update failure is reported without stopping other components");
        replacement.DuringUpdate = null;
        FailingAwakeProbe.Destroyed = 0;
        Throws<InvalidOperationException>(() => host.AddComponent<FailingAwakeProbe>(), "Awake failure must propagate");
        Check(host.GetComponent<FailingAwakeProbe>() == null && FailingAwakeProbe.Destroyed == 1, "Failed Awake rolls back attachment");

        host.Dispose();
        world.Update(0.1);
        world.LateUpdate();
        Check(replacement.IsDisposed && added.IsDisposed && replacement.Destroys == 1 && added.Destroys == 1,
            "Recursive disposal destroys children and components");
        Throws<InvalidOperationException>(() => new EntitySystemRegistry(new[] { typeof(LifecycleProbeSystem), typeof(LifecycleProbeSystem) }),
            "Duplicate lifecycle binding must fail at registration");
        CheckInvalidSignature();
        CheckDisposalDuringUpdate();
        Console.WriteLine("PASS: automatic lifecycle binding, parent constraints, duplicate rejection, deferred registration, pause, disposal, exception isolation, startup validation.");
    }

    private static void CheckDisposalDuringUpdate()
    {
        using (var world = new EcsWorld())
        {
            LifecycleProbe first = world.Root.AddChild<LifecycleHost>().AddComponent<LifecycleProbe>();
            LifecycleHost laterHost = world.Root.AddChild<LifecycleHost>();
            LifecycleProbe later = laterHost.AddComponent<LifecycleProbe>();
            first.DuringUpdate = _ => laterHost.Dispose();
            world.Update(0.1);
            world.LateUpdate();
            Check(first.Updates == 1 && later.Updates == 0 && later.LateUpdates == 0 && later.Destroys == 1,
                "Disposal during dispatch prevents the later component from running");
        }
        var disposedWorld = new EcsWorld();
        LifecycleProbe shutdown = disposedWorld.Root.AddChild<LifecycleHost>().AddComponent<LifecycleProbe>();
        LifecycleProbe skipped = disposedWorld.Root.AddChild<LifecycleHost>().AddComponent<LifecycleProbe>();
        shutdown.DuringUpdate = _ => disposedWorld.Dispose();
        disposedWorld.Update(0.1);
        Check(disposedWorld.IsDisposed && shutdown.Destroys == 1 && skipped.Destroys == 1 && skipped.Updates == 0,
            "World shutdown during dispatch clears the hierarchy and skips remaining updates");
    }

    private static void CheckInvalidSignature()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("InvalidEcsFixture"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Fixture");
        var type = module.DefineType("InvalidSystem", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        type.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntitySystemOfAttribute).GetConstructor(new[] { typeof(Type) }), new object[] { typeof(LifecycleProbe) }));
        var method = type.DefineMethod("Update", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(Entity) });
        method.SetCustomAttribute(new CustomAttributeBuilder(typeof(EntitySystemAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
        method.SetCustomAttribute(new CustomAttributeBuilder(typeof(ExtensionAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()));
        method.GetILGenerator().Emit(OpCodes.Ret);
        Type invalid = type.CreateType();
        Throws<InvalidOperationException>(() => new EntitySystemRegistry(new[] { invalid }), "Wrong lifecycle component signature must fail at startup");
    }
}

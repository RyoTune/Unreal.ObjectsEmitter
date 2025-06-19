using System.Runtime.InteropServices;
using UE.Toolkit.Core.Types.Unreal.UE5_4_4;
using UE.Toolkit.Interfaces;
using Unreal.ObjectsEmitter.Interfaces;
using Unreal.ObjectsEmitter.Interfaces.Types;
using EFindName = Unreal.ObjectsEmitter.Interfaces.Types.EFindName;
using FName = Unreal.ObjectsEmitter.Interfaces.Types.FName;
using FNamePool = Unreal.ObjectsEmitter.Interfaces.Types.FNamePool;
using FString = Unreal.ObjectsEmitter.Interfaces.Types.FString;
using IDataTables = Unreal.ObjectsEmitter.Interfaces.IDataTables;

namespace Unreal.ObjectsEmitter.Reloaded;

public unsafe class ObjectEmitterShim : IUnreal, IUObjects, IDataTables
{
    private readonly IUnrealMemory _mem;
    private readonly IUnrealObjects _objs;
    private readonly IUnrealNames _names;
    private readonly UE.Toolkit.Interfaces.IDataTables _dt;
    private FNamePool* _gNamePool;

    public ObjectEmitterShim(IUnrealMemory mem, IUnrealObjects objs, IUnrealNames names, UE.Toolkit.Interfaces.IDataTables dt)
    {
        _mem = mem;
        _objs = objs;
        _names = names;
        _dt = dt;

        ScanHooks.Add(
            "FGlobalNamePool",
            "4C 8D 05 ?? ?? ?? ?? EB ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 ?? ?? ?? ?? 01 48 8B 44 24 ?? 48 8B D3 48 C1 E8 20 8D 0C ?? 49 03 4C ?? ?? E8 ?? ?? ?? ?? 48 8B C3",
            (_, result) =>
            {
                _gNamePool = (FNamePool*)GetGlobalAddress(result + 3);
            });

        _objs.OnObjectLoaded += uobj => ObjectCreated?.Invoke(new(uobj.Name, (UObject*)uobj.Self));
    }

    #region IUnreal

    public FName* FName(string str, EFindName findType = EFindName.FName_Add)
    {
        var name = new UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName(str, (UE.Toolkit.Core.Types.Unreal.UE5_4_4.EFindName)findType);
        var finalName = (UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName*)Marshal.AllocHGlobal(
            sizeof(UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName));
        
        *finalName = name;
        
        return (FName*)finalName;
    }

    public string GetName(FName* name) => ((UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName*)name)->ToString();

    public string GetName(FName name) => ((UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName*)&name)->ToString();

    public string GetName(uint poolLoc)
    {
        UE.Toolkit.Core.Types.Unreal.UE5_4_4.FName tempName = new();
        tempName.ComparisonIndex.Value = poolLoc;
        return tempName.ToString();
    }

    public void AssignFName(string modName, string fnameString, string newString)
        => _names.RedirectFName(modName, fnameString, newString);

    public nint FMalloc(long size, int alignment) => _mem.Malloc((nint)size, alignment);

    public FString FString(string str)
    {
        var fstr = (FString*)_objs.CreateFString(str);
        return *fstr;
    }

    public FNamePool* GetPool() => _gNamePool;
    
    private static nuint GetGlobalAddress(nint ptrAddress) => (nuint)(*(int*)ptrAddress + ptrAddress + 4);

    #endregion

    #region IUObjects
    
    public Action<UnrealObject>? ObjectCreated { get; set; }

    public void FindObject(string objectName, Action<UnrealObject> found, Func<UnrealObject, bool>? isReady = null)
        => _objs.OnObjectLoadedByName<UObjectBase>(objectName, obj => found(new(objectName, (UObject*)obj.Self)));

    #endregion

    #region IDataTables
    
    public bool TryGetDataTable(string tableName, out DataTable? dataTable) => throw new NotImplementedException("Unreal Toolkit does not currently support this.");

    public void FindDataTable(string tableName, Action<DataTable> found)
        => _dt.OnDataTableChanged<UObjectBase>(tableName, table => found(new(table.Name, (UDataTable*)table.Self, table.Values.Select(x => new Row(x.Name, (UObject*)x.Value)).ToArray())));

    public void FindDataTable<TRow>(string tableName, Action<DataTable<TRow>> found) where TRow : unmanaged
        => _dt.OnDataTableChanged<TRow>(tableName, table =>
        {
            found(new(table.Name, (UDataTable*)table.Self, table.Values.Select(x => new Row<TRow>(x.Name, x.Value)).ToArray()));
        });

    public Action<DataTable>? DataTableFound
    {
        get => throw new NotImplementedException("Unreal Toolkit does not currently support this.");
        set => throw new NotImplementedException("Unreal Toolkit does not currently support this.");
    }
    
    public DataTable[] GetDataTables() => throw new NotImplementedException("Unreal Toolkit does not currently support this.");

    #endregion
}
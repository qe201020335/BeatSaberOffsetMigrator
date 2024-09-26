using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace BeatSaberOffsetMigrator.Shared;

public class OVRHelperSharedMemoryManager
{
    private const string MemoryMappedFileName = "BeatSaberOffsetMigrator.OVRHelperSharedMemory";
    
    private readonly long _size = Marshal.SizeOf(typeof(ControllerPose)); 

    private readonly MemoryMappedFile _memoryMappedFile;

    private readonly MemoryMappedViewAccessor _memoryMappedViewAccessor;

    private readonly bool _readOnly;

    private OVRHelperSharedMemoryManager(bool readOnly)
    {
        _readOnly = readOnly;
        var access = readOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite;
        _memoryMappedFile = MemoryMappedFile.CreateOrOpen(MemoryMappedFileName, _size, access);
        _memoryMappedViewAccessor = _memoryMappedFile.CreateViewAccessor(0, 0, access);
    }
    
    ~OVRHelperSharedMemoryManager()
    {
        _memoryMappedViewAccessor.Dispose();
        _memoryMappedFile.Dispose();
    }
    
    public static OVRHelperSharedMemoryManager CreateReadOnly()
    {
        return new OVRHelperSharedMemoryManager(true);
    }
    
    public static OVRHelperSharedMemoryManager CreateWriteOnly()
    {
        return new OVRHelperSharedMemoryManager(false);
    }
    
    public void Write(ref ControllerPose pose)
    {
        // if (_readOnly) throw new InvalidOperationException("Cannot write to read-only memory mapped file");
        _memoryMappedViewAccessor.Write(0, ref pose);
    }
    
    public ControllerPose Read(ref ControllerPose pose)
    {
        _memoryMappedViewAccessor.Read<ControllerPose>(0, out pose);
        return pose;
    }
}
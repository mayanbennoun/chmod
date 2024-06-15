using System.Collections.Concurrent;
using Microsoft.Extensions.Options;


namespace chmodPermissions
{
	public class ChmodPermissionManager : IRegister
	{
		private readonly ConcurrentDictionary<string, int> _refCounts = new ();// References by path
		private readonly ConcurrentDictionary<string, string> _originalPermissions = new(); // File original permissions 
		private readonly ConcurrentDictionary<string, string> _currentPermissions =new(); // File current permissions  
		private readonly ConcurrentDictionary<int, (string path, bool read, bool write)> _handles = new();
		private readonly object _lock = new();
		private int _nextHandle;
		private readonly FilePermissionsSettings _fileSettings;

		public ChmodPermissionManager(IOptions<FilePermissionsSettings> filePermissionsOptions)
		{
			_fileSettings = filePermissionsOptions.Value;
			_nextHandle = 1;
		}

		public int Register(string path, bool read, bool write)
		{
			lock (_lock)
			{
				if (!_originalPermissions.ContainsKey(path)) //store orignal permissions to be restored 
				{
					_originalPermissions[path] = GetPermissioms(path);
					_currentPermissions[path] = _originalPermissions[path];
				}

				_refCounts.AddOrUpdate(path, 1, (key, count) => count + 1); // add ref 
				string newPermissions = _currentPermissions[path];
				if (read && newPermissions[_fileSettings.ReadIndex] != 'r')
				{
					newPermissions = newPermissions.Remove(_fileSettings.ReadIndex, 1).Insert(_fileSettings.ReadIndex, "r");
				}
				if (write && newPermissions[_fileSettings.WriteIndex] != 'w')
				{
					newPermissions = newPermissions.Remove(_fileSettings.WriteIndex, 1).Insert(_fileSettings.WriteIndex, "w");
				}
				_currentPermissions[path] = newPermissions;

				int handle = _nextHandle++;
				_handles[handle]=(path,read, write);
				Console.WriteLine($"After register  {handle} pernissions{_currentPermissions[path]}");
				return handle;
			}

		}

		public void Unregister(int handle)
		{
			lock (_lock)
			{
				if (!_handles.TryRemove(handle, out var handleInfo))
				{
					throw new InvalidOperationException("Invalid Handle");
				}
				var (path, read, write) = handleInfo;
				//Console.WriteLine($"Before Unregisster {handle} pernissions{_currentPermissions[path]}");
				if (_refCounts.AddOrUpdate(path,0,(key ,count)=> count - 1) == 0)
				{
					_refCounts.TryRemove(path, out _);
					_currentPermissions [path] = _originalPermissions[path];    
					_originalPermissions.TryRemove(path, out _);
					Console.WriteLine($"Permission after Unregister handle{handle} :{_currentPermissions[path]}");
				}
				else
				{
					// Adjust current permissions based on remaining handles
					bool anyRead = false;
					bool anyWrite = false;
					foreach (var entry in _handles)
					{
						if (entry.Value.path == path)
						{
							if (entry.Value.read) anyRead = true;
							if (entry.Value.write) anyWrite = true;
						}
					}

					string newPermissions = _currentPermissions[path];

					if (!anyRead && newPermissions[_fileSettings.ReadIndex] == 'r')
					{
						newPermissions = newPermissions.Remove(_fileSettings.ReadIndex, 1).Insert(_fileSettings.ReadIndex, "-");
					}
					if (!anyWrite && newPermissions[_fileSettings.WriteIndex] == 'w')
					{
						newPermissions = newPermissions.Remove(_fileSettings.WriteIndex, 1).Insert(_fileSettings.WriteIndex, "-");
					}

					_currentPermissions[path] = newPermissions;
					Console.WriteLine($"Permission after Unregister handle{handle} :{_currentPermissions[path]}");
				}
			}
		}

		private string GetPermissioms(string path)
		{
			return _fileSettings.DefaultPermissions;
		}
	}
}

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;


namespace chmodPermissions
{
	/// <summary>
	/// Class that handles chmod permissions
	/// </summary>
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

		/// <summary>
		/// Register new handle to file
		/// </summary>
		/// <param name="path"> file path</param>
		/// <param name="read">others read permission</param>
		/// <param name="write"> others write permission</param>
		/// <returns> new handle index </returns>
		public int Register(string path, bool read, bool write)
		{
			lock (_lock)
			{
				if (!_originalPermissions.ContainsKey(path)) //store orignal permissions to be restored 
				{
					_originalPermissions[path] = GetDefaultPermissioms();
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
				return handle;
			}

		}

		/// <summary>
		/// Unregister existing file handle
		/// </summary>
		/// <param name="handle"> handle index to remove</param>
		/// <exception cref="InvalidOperationException">raised if given an Invalid handler</exception>
		public void Unregister(int handle)
		{
			lock (_lock)
			{
				if (!_handles.TryRemove(handle, out var handleInfo))
				{
					throw new InvalidOperationException("Invalid Handle");
				}
				var (path, read, write) = handleInfo;
				if (_refCounts.AddOrUpdate(path,0,(key ,count)=> count - 1) == 0)
				{
					_refCounts.TryRemove(path, out _);
					_currentPermissions [path] = _originalPermissions[path];    
					_originalPermissions.TryRemove(path, out _);
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
				}
			}
		}

		/// <summary>
		/// fetch file default permission by path 
		/// </summary>
		/// <returns></returns>
		private string GetDefaultPermissioms()
		{
			return _fileSettings.DefaultPermissions ;
		}
	}
}

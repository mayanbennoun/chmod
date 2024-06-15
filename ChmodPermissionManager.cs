using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace chmodPermissions
{
	public class ChmodPermissionManager : IRegister
	{
		private readonly ConcurrentDictionary<string, int> _refCounts = []; //refs by path 
		private readonly ConcurrentDictionary<string, string> _originalPermissions = []; // file orignal permissions 
		private readonly ConcurrentDictionary<string, string> _currentPermissions = []; // file current permissions 
		private readonly ConcurrentDictionary<int, (string path, bool read, bool write)> _handles = [];
		private readonly object _lock = new object();
		private int _nextHandle = 1;

		public int Register(string path, bool read, bool write)
		{
			lock (_lock)
			{
				if (!_originalPermissions.ContainsKey(path)) //store orignal permissions to be rstored 
				{
					_originalPermissions[path] = GetPermissioms(path);
					_currentPermissions[path] = _originalPermissions[path];
				}
				//Console.WriteLine($"Before register pernissions{_currentPermissions[path]}");

				_refCounts.AddOrUpdate(path, 1, (key, count) => count + 1); // add ref 
				string newPermissions = _currentPermissions[path];
				if (read & newPermissions[7] != 'r')
				{
					newPermissions = newPermissions.Remove(7,1).Insert(7, "r");
				}
				 if (write & newPermissions[8] != 'w')
				{
					newPermissions = newPermissions.Remove(8,1).Insert(8, "w");
				}
				_currentPermissions[path] = newPermissions;
				int handle = _nextHandle++;
				_handles[handle]=(path,read, write);
				Console.WriteLine($"After register  {handle}pernissions{_currentPermissions[path]}");
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
					_handles.TryRemove(handle, out _);
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

					if (!anyRead && newPermissions[7] == 'r')
					{
						newPermissions = newPermissions.Remove(7, 1).Insert(7, "-");
					}
					if (!anyWrite && newPermissions[8] == 'w')
					{
						newPermissions = newPermissions.Remove(8, 1).Insert(8, "-");
					}

					_currentPermissions[path] = newPermissions;
					Console.WriteLine($"Permission after Unregister handle{handle} :{_currentPermissions[path]}");
				}
			}
		}

		private string GetPermissioms(string path)
		{
			return "-rw-r-----";
		}
	}
}

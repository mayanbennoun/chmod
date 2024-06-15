namespace chmodPermissions
{
	public interface IRegister
	{
		/// <summary>
		/// Register  refrence to path and add relevent permissions 
		/// </summary>
		/// <param name="path"> file path</param>
		/// <param name="read"> other read permissions </param>
		/// <param name="write"> other write permissions</param>
		/// <returns></returns>
		int Register(string path, bool read, bool write);

		/// <summary>
		/// unregister handle to file remove handle permissions
		/// </summary>
		/// <param name="handle"> handle identifier </param>
		void Unregister(int handle);
	}
}

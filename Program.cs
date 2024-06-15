
using chmodPermissions;

var manager = new ChmodPermissionManager();

int handle1 = manager.Register("secret.txt", true, false);
int handle2 = manager.Register("secret.txt", false, true);
manager.Unregister(handle1);
manager.Unregister(handle2);
int handle3 = manager.Register("secret.txt", true, true);
int handle4 = manager.Register("secret.txt", true, false);
manager.Unregister(handle3);
manager.Unregister(handle4);
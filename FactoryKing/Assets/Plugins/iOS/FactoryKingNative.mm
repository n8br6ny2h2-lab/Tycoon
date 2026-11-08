// Factory King native helpers for iOS: iCloud key-value save sync and optional local notifications.
// Called from FactoryKing.Runtime.Platform.NativeBridge via DllImport("__Internal").
#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>
#include <stdlib.h>
#include <string.h>

static char* FKCopyString(NSString* s)
{
    if (s == nil) return NULL;
    const char* utf8 = [s UTF8String];
    char* out = (char*)malloc(strlen(utf8) + 1);
    strcpy(out, utf8);
    return out;
}

extern "C" {

bool FK_CloudAvailable()
{
    return [[NSFileManager defaultManager] ubiquityIdentityToken] != nil;
}

void FK_CloudSet(const char* key, const char* value)
{
    if (key == NULL || value == NULL) return;
    NSUbiquitousKeyValueStore* store = [NSUbiquitousKeyValueStore defaultStore];
    [store setString:[NSString stringWithUTF8String:value] forKey:[NSString stringWithUTF8String:key]];
    [store synchronize];
}

char* FK_CloudGet(const char* key)
{
    if (key == NULL) return NULL;
    NSUbiquitousKeyValueStore* store = [NSUbiquitousKeyValueStore defaultStore];
    [store synchronize];
    return FKCopyString([store stringForKey:[NSString stringWithUTF8String:key]]);
}

void FK_FreeString(char* p)
{
    if (p != NULL) free(p);
}

void FK_NotificationsRequest()
{
    [[UNUserNotificationCenter currentNotificationCenter]
        requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound | UNAuthorizationOptionBadge)
        completionHandler:^(BOOL granted, NSError* _Nullable error) {}];
}

void FK_NotificationSchedule(const char* identifier, const char* title, const char* body, double seconds)
{
    if (identifier == NULL || seconds < 1) return;
    UNMutableNotificationContent* content = [[UNMutableNotificationContent alloc] init];
    content.title = [NSString stringWithUTF8String:title ? title : ""];
    content.body = [NSString stringWithUTF8String:body ? body : ""];
    content.sound = [UNNotificationSound defaultSound];
    UNTimeIntervalNotificationTrigger* trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:seconds repeats:NO];
    UNNotificationRequest* request = [UNNotificationRequest requestWithIdentifier:[NSString stringWithUTF8String:identifier]
                                                                          content:content trigger:trigger];
    [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:request withCompletionHandler:nil];
}

void FK_NotificationsCancelAll()
{
    [[UNUserNotificationCenter currentNotificationCenter] removeAllPendingNotificationRequests];
}

}

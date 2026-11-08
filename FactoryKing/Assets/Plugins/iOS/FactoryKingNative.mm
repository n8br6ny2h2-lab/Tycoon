// Factory King native helpers for iOS: iCloud key-value save sync and optional local notifications.
// Called from FactoryKing.Runtime.Platform.NativeBridge via DllImport("__Internal").
#import <Foundation/Foundation.h>
#import <UserNotifications/UserNotifications.h>
#import <UIKit/UIKit.h>
#import <StoreKit/StoreKit.h>
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

// 0 = light, 1 = medium, 2 = heavy, 3 = success, 4 = warning, 5 = error, 6 = selection
void FK_Haptic(int style)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (style <= 2)
        {
            UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleLight : (style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy);
            UIImpactFeedbackGenerator* g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
            [g impactOccurred];
        }
        else if (style <= 5)
        {
            UINotificationFeedbackGenerator* g = [[UINotificationFeedbackGenerator alloc] init];
            UINotificationFeedbackType t = style == 3 ? UINotificationFeedbackTypeSuccess : (style == 4 ? UINotificationFeedbackTypeWarning : UINotificationFeedbackTypeError);
            [g notificationOccurred:t];
        }
        else
        {
            UISelectionFeedbackGenerator* g = [[UISelectionFeedbackGenerator alloc] init];
            [g selectionChanged];
        }
    });
}

bool FK_LowPowerMode()
{
    return [[NSProcessInfo processInfo] isLowPowerModeEnabled];
}

// Apple decides whether the rating dialog is actually shown (max. 3 times a year).
void FK_RequestReview()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UIWindowScene* scene = nil;
        for (UIScene* s in [UIApplication sharedApplication].connectedScenes)
            if ([s isKindOfClass:[UIWindowScene class]] && s.activationState == UISceneActivationStateForegroundActive) { scene = (UIWindowScene*)s; break; }
        if (scene != nil) [SKStoreReviewController requestReviewInScene:scene];
    });
}

void FK_NotificationsCancelAll()
{
    [[UNUserNotificationCenter currentNotificationCenter] removeAllPendingNotificationRequests];
}

}

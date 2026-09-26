# Private Listing Chat

## Scope

Part of the initial MVP in [PRODUCT_SCOPE.md](../PRODUCT_SCOPE.md). Provide private conversations between the listing owner and an interested user about a specific listing, with text messages, photo attachments, and restricted message editing.

## Conversations and Access

- A conversation belongs to one listing and exactly two distinct application users: the listing owner and the interested user.
- Reopening chat for the same listing and participant pair reuses their conversation. Different interested users have separate conversations; the same pair discussing another listing also has a separate conversation.
- Users must be signed in to participate. Browsing a public listing does not grant access to its conversations or attachments.
- Only the two participants can read or send messages and upload or retrieve chat photos through application features. Enforce access on the server for each operation.
- Both participants can reopen the conversation and see its listing context and ordered message history.

## Messages and Photos

- Each participant can send text, photos, or text with photos. An empty message without an attachment is invalid.
- Store message content, sender, ordering, timestamps, photo metadata, and external attachment references in the database. Store every chat-photo file and derived variant outside the database with private access under the selected [photo-storage design](PhotoStorage.md), including its quotas and budget limits. Do not store image bytes or Base64 image content in message or attachment records.
- Chat attachments must not be exposed through public listing-image URLs or an unrestricted public CDN. Use participant-authorized delivery.
- Show sending/upload progress and failure states. A failed or incomplete send must not appear as a successfully sent message or change which message is editable.

## Last-Message Editing Rule

A message can be edited only by its sender, and only while it is the newest successfully sent message in that conversation.

- If a user sends several messages in succession, only the newest one is editable.
- When the other participant sends a text or photo message, the earlier message is no longer editable.
- Typing, reading, starting an upload, or an unsuccessful send does not end the editing opportunity.
- Sending another message later does not make an earlier message editable again.
- Editing updates the existing message without moving it in the conversation order. Show an edited indicator.
- Apply the eligibility check and edit atomically on the server using the conversation's authoritative message order. If a newer message has already been accepted, reject the edit even when the sender's screen is stale.
- When an edit is rejected because another message arrived, preserve the unsaved text and explain that the message can no longer be edited.

Text and photo captions follow this rule. Whether an eligible edit can also replace or remove attached photos remains a decision for the detailed attachment workflow.

### Examples

| Conversation order | Editable message |
|---|---|
| A sends M1 | A may edit M1. |
| A sends M1, then M2 | A may edit M2; M1 is locked. |
| A sends M1, B replies M2 | B may edit M2; A cannot edit M1. |
| A sends M1, B replies with photo M2 | B may edit M2's caption; M1 is locked. |
| A sends M1, B starts typing or has a failed send | A may still edit M1. |
| A sends M1, B replies M2, A sends M3 | Only A may edit M3; M1 and M2 stay locked. |

## Acceptance Criteria

- Two signed-in users can open a conversation from a listing, exchange text and photos, and reopen the same history later.
- A third user cannot read the conversation, send into it, or retrieve its photo attachments, including through direct API or attachment requests.
- Different listing/participant combinations do not share messages or attachments.
- Editing behavior matches the examples, including stale clients and concurrent sends/edits.
- Both participants see the saved edit and its edited indicator after synchronization.
- Failed uploads and sends preserve an accurate conversation state and can be retried without creating duplicate messages.

## Decisions Before Implementation

- Photo count/size limits per message, accepted formats, and attachment replacement/removal during editing.
- Message/photo retention and access when a listing is unpublished/deleted or a participant's account is removed.
- Message delivery/update mechanism and history pagination.
- How registration-verification state controls starting and using chats.

Keep these decisions in the feature specification as the roadmap is expanded. They do not add group chat, typing indicators, read receipts, or external message notifications to the current feature scope.

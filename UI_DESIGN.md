# UI Design for Feedback Sorter

This is a demo / PoC app, and will not be used in production. Therefore some typical aspects such as authentication/authorization are not considered.

There will be two main views for the feedback sorter:

- **Feedback submission view**: This allows submission of feedback.
- **Submitted Feedback browser**: This allows browsing the feedback submissions and their analysis results.

## The Feedback Submission View

This will be a very simple view with a large, multi-line text box for the user feedback, and a submit button.
When user presses the submit button, and there is more than five characters in the text box, the feedback
is sent to the backend.
If backend indicates success the text box is cleared. A toast popup will indicate that submission was done.
Otherwise, text box is not cleared and a toast error pop-up is shown.

## The Submitted Feedback browser

This view will have two lists showing feedbacks:
- The successfully analyzed feedbacks list
- The unsuccessfully analyzed feedbacks list

both lists will be paged.

### The Successfully Analyzed feedbacks list

Each row on this list will have the following information:
- Title (a short summary generated by the AI)
- Sentiment ('Neutral', 'Mixed', 'Positive' or 'Negative')
- Feedback Categories ('Bug report', 'Feature Request', 'General'). There may be more than one Feature types per row
- Feature Categories. A list of features the feedback concerns. Can have zero or more features (such as "Login page", "Settings")
- A 'refresh' button/icon that will mark the feedback for re-analysis.

The full feedback can be viewed (in a modal pop up) by clicking the title.

### The Failed to Analyze feedbacks list

Each row on this list will have the following information:
- The first 20 characters of the feedback, with all sequences of whitespace replaced with single space character.
- A 'refresh' button for flagging the feedback for re-analysis

## Other Considerations

- Since this is a demo app, allow defining the base url (default: "http://localhost:5225") in the app.
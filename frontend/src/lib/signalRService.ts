import * as signalR from "@microsoft/signalr";

export type FeedbackAnalyzedHandler = (feedbackId: string) => void;
export type FeedbackAnalysisFailedHandler = (feedbackId: string) => void;

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnecting = false;
  private isInitialized = false;

  private initializeConnection() {
    if (!this.isInitialized) {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl("http://localhost:5225/feedbackHub")
        .withAutomaticReconnect([0, 2000, 10000, 30000])
        .build();

      this.connection.onreconnecting(() => {
        console.log("SignalR reconnecting...");
      });

      this.connection.onreconnected(() => {
        console.log("SignalR reconnected");
        // Rejoin dashboard group on reconnection
        this.joinDashboardGroup();
      });

      this.connection.onclose(() => {
        console.log("SignalR connection closed");
      });

      this.isInitialized = true;
    }
  }

  private async joinDashboardGroup() {
    if (
      this.connection &&
      this.connection.state === signalR.HubConnectionState.Connected
    ) {
      try {
        await this.connection.invoke("JoinDashboard");
        console.log("Joined Dashboard group");
      } catch (err) {
        console.error("Error joining dashboard group: ", err);
      }
    }
  }

  async connect(): Promise<void> {
    this.initializeConnection();

    if (!this.connection) {
      console.error("SignalR connection not initialized");
      return;
    }

    if (this.connection.state === signalR.HubConnectionState.Connected) {
      console.log("SignalR already connected");
      await this.joinDashboardGroup();
      return;
    }

    if (this.isConnecting) {
      console.log("SignalR connection already in progress");
      return;
    }

    // Only attempt to connect if we're disconnected
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      try {
        this.isConnecting = true;
        console.log("Starting SignalR connection...");
        await this.connection.start();
        console.log("SignalR Connected");

        // Join the Dashboard group only after successful connection
        await this.joinDashboardGroup();
      } catch (err) {
        console.error("SignalR Connection Error: ", err);
      } finally {
        this.isConnecting = false;
      }
    }
  }

  async disconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    // Don't fully disconnect, just leave the dashboard group
    // This allows the connection to persist across navigation
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke("LeaveDashboard");
        console.log("Left Dashboard group");
      } catch (err) {
        console.error("Error leaving dashboard group: ", err);
      }
    }
  }

  async forceDisconnect(): Promise<void> {
    if (!this.connection) {
      return;
    }

    if (this.connection.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke("LeaveDashboard");
        console.log("Left Dashboard group");
      } catch (err) {
        console.error("Error leaving dashboard group: ", err);
      }
    }

    if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
      try {
        await this.connection.stop();
        console.log("SignalR Disconnected");
        this.isInitialized = false;
        this.connection = null;
      } catch (err) {
        console.error("SignalR Disconnection Error: ", err);
      }
    }
  }

  onFeedbackAnalyzed(handler: FeedbackAnalyzedHandler): void {
    if (this.connection) {
      this.connection.on("FeedbackAnalyzed", handler);
    }
  }

  onFeedbackAnalysisFailed(handler: FeedbackAnalysisFailedHandler): void {
    if (this.connection) {
      this.connection.on("FeedbackAnalysisFailed", handler);
    }
  }

  offFeedbackAnalyzed(handler: FeedbackAnalyzedHandler): void {
    if (this.connection) {
      this.connection.off("FeedbackAnalyzed", handler);
    }
  }

  offFeedbackAnalysisFailed(handler: FeedbackAnalysisFailedHandler): void {
    if (this.connection) {
      this.connection.off("FeedbackAnalysisFailed", handler);
    }
  }

  get connectionState(): signalR.HubConnectionState | null {
    return this.connection?.state ?? null;
  }

  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// Singleton instance
export const signalRService = new SignalRService();

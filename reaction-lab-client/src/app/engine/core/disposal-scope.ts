export interface Disposable {
    dispose(): void;
}

export class DisposalScope implements Disposable {
    private readonly owned: Disposable[] = [];
    private disposed = false;

    add<T extends Disposable>(disposable: T): T {
        if (this.disposed) {
            disposable.dispose();
        } else {
            this.owned.push(disposable);
        }

        return disposable;
    }

    dispose(): void {
        if (this.disposed) {
            return;
        }

        this.disposed = true;

        for (const disposable of [...this.owned].reverse()) {
            disposable.dispose();
        }

        this.owned.length = 0;
    }
}
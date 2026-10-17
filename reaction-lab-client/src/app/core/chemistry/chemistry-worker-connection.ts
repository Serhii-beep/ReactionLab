import { defer, first, map, Observable, ReplaySubject, Subject } from "rxjs";
import { ChemistryRequest, ChemistryWorkerMessage } from "./chemistry-contracts";

type ChemistryAnswer = Extract<ChemistryWorkerMessage, { readonly id: number }>;

const LOAD_FAILURE = 'The chemistry worker failed to load';

export class ChemistryWorkerConnection {
    private readonly worker = new Worker(new URL('./chemistry.worker', import.meta.url), { type: 'module' });
    private readonly readiness$ = new ReplaySubject<void>(1);
    private readonly answers$ = new Subject<ChemistryAnswer>();
    private nextId = 0;

    readonly ready$ = this.readiness$.asObservable();

    constructor() {
        this.worker.addEventListener('message', ({ data }: MessageEvent<ChemistryWorkerMessage>) => this.receive(data));
        this.worker.addEventListener('error', (event: Event) => this.readiness$.error(new Error(event instanceof ErrorEvent ? event.message : LOAD_FAILURE)));
    }

    send(request: ChemistryRequest): Observable<string | null> {
        return defer(() => {
            const id = this.nextId++;

            this.worker.postMessage({ ...request, id });

            return this.answers$.pipe(
                first((answer) => answer.id === id),
                map((answer) => {
                    if (answer.type === 'error') {
                        throw new Error(answer.message);
                    }

                    return answer.json;
                })
            );
        });
    }

    terminate(): void {
        this.worker.terminate();
        this.answers$.complete();
    }

    private receive(message: ChemistryWorkerMessage): void {
        if (message.type === 'ready') {
            this.readiness$.next();
            this.readiness$.complete();
        } else if (message.type === 'failed') {
            this.readiness$.error(new Error(message.message));
        } else {
            this.answers$.next(message);
        }
    }
}
import { ChemistryRequest, ChemistryWorkerMessage, ChemistryWorkerRequest } from "./chemistry-contracts";

interface ChemistryExports {
  LoadReferenceData(elementsJson: string, referenceJson: string): void;
  ParseFormula(formula: string): string;
  MolarMassOf(formula: string): string;
  Balance(requestJson: string): string;
  Assemble(requestJson: string): string;
  DescribeSketch(requestJson: string): string;
  StructureKey(requestJson: string): string;
  DetectSymmetry(requestJson: string): string;
  Predict(requestJson: string): string;
  DescribeConditions(requestJson: string): string;
}

interface AssemblyExports {
  readonly ReactionLab: { readonly Chemistry: { readonly Browser: { readonly ChemistryExports: ChemistryExports } } };
}

interface DotnetRuntime {
  getConfig(): { readonly mainAssemblyName: string };
  getAssemblyExports(assemblyName: string): Promise<AssemblyExports>;
}

interface DotnetModule {
  readonly dotnet: { create(): Promise<DotnetRuntime> };
}

type JsonOperation = Extract<ChemistryRequest, { readonly requestJson: string }>['operation'];

const RUNTIME_PATH = 'chemistry/_framework/dotnet.js';

const JSON_OPERATIONS: Readonly<Record<JsonOperation, (exports: ChemistryExports, requestJson: string) => string>> = {
  balance: (exports, requestJson) => exports.Balance(requestJson),
  assemble: (exports, requestJson) => exports.Assemble(requestJson),
  describeSketch: (exports, requestJson) => exports.DescribeSketch(requestJson),
  structureKey: (exports, requestJson) => exports.StructureKey(requestJson),
  detectSymmetry: (exports, requestJson) => exports.DetectSymmetry(requestJson),
  predict: (exports, requestJson) => exports.Predict(requestJson),
  describeConditions: (exports, requestJson) => exports.DescribeConditions(requestJson)
};

const chemistryExports = boot();

addEventListener('message', ({ data }: MessageEvent<ChemistryWorkerRequest>) => {
  void respond(data);
});

chemistryExports.then(
  () => post({ type: 'ready' }),
  (error: unknown) => post({ type: 'failed', message: messageOf(error) })
);

async function boot(): Promise<ChemistryExports> {
  const runtimeModule = await import(/* @vite-ignore */ new URL(RUNTIME_PATH, self.location.href).href) as DotnetModule;
  const runtime = await runtimeModule.dotnet.create();
  const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);

  return exports.ReactionLab.Chemistry.Browser.ChemistryExports;
}

async function respond(request: ChemistryWorkerRequest): Promise<void> {
  try {
    post({ type: 'result', id: request.id, json: invoke(await chemistryExports, request) });
  } catch (error) {
    post({ type: 'error', id: request.id, message: messageOf(error) });
  }
}

function invoke(exports: ChemistryExports, request: ChemistryRequest): string | null {
  switch (request.operation) {
    case 'loadReferenceData':
      exports.LoadReferenceData(request.elementsJson, request.referenceJson);
      return null;
    case 'parseFormula':
      return exports.ParseFormula(request.formula);
    case 'molarMassOf':
      return exports.MolarMassOf(request.formula);
    default:
      return JSON_OPERATIONS[request.operation](exports, request.requestJson);
  }
}

function post(message: ChemistryWorkerMessage): void {
  postMessage(message);
}

function messageOf(error: unknown): string {
  return typeof error === 'object' && error !== null && 'message' in error ? String(error.message) : String(error);
}
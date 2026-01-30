import { MatterState } from "./element.model";

export interface Molecule {
    id: string;
    formula: string;
    name: string;
    iupacName: string | null;
    commonNames: string | null;
    molecularWeight: number | null;
    structure3D: string | null;
    isOrganic: boolean;
    category: string | null;
    stateAtRoomTemp: MatterState;
    description: string | null;
    uses: string | null;
    safetyInfo: string | null;
    interestingFacts: string | null;
    imageUrl: string | null;
    model3DUrl: string | null;
    elements: MoleculeElement[];
}

export interface MoleculeSummary {
    id: string;
    formula: string;
    name: string;
    molecularWeight: number | null;
    isOrganic: boolean;
    category: string | null;
    stateAtRoomTemp: MatterState;
}

export interface MoleculeElement {
    elementId: string;
    symbol: string;
    name: string;
    count: number;
}

export interface CreateMolecule {
    formula: string;
  name: string;
  iupacName?: string;
  commonNames?: string;
  molecularWeight?: number;
  structure3D?: string;
  isOrganic: boolean;
  category?: string;
  stateAtRoomTemp: MatterState;
  description?: string;
  uses?: string;
  safetyInfo?: string;
  interestingFacts?: string;
  imageUrl?: string;
  model3DUrl?: string;
  elements?: CreateMoleculeElement[];
}

export interface CreateMoleculeElement {
    elementId: string;
    count: number;
}
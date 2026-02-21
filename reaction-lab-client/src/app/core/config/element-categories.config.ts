import { ElementCategory } from '../models';

export interface CategoryConfig {
  value: ElementCategory;
  label: string;
  color: string;
}

export const ELEMENT_CATEGORIES: CategoryConfig[] = [
  { value: ElementCategory.AlkaliMetal, label: 'Alkali Metal', color: '#ff6b6b' },
  { value: ElementCategory.AlkalineEarthMetal, label: 'Alkaline Earth', color: '#ffa94d' },
  { value: ElementCategory.TransitionMetal, label: 'Transition Metal', color: '#ffd43b' },
  { value: ElementCategory.PostTransitionMetal, label: 'Post-Transition', color: '#69db7c' },
  { value: ElementCategory.Metalloid, label: 'Metalloid', color: '#4dabf7' },
  { value: ElementCategory.NonMetal, label: 'Nonmetal', color: '#9775fa' },
  { value: ElementCategory.Halogen, label: 'Halogen', color: '#f783ac' },
  { value: ElementCategory.NobleGas, label: 'Noble Gas', color: '#a9e34b' },
  { value: ElementCategory.Lanthanide, label: 'Lanthanide', color: '#66d9e8' },
  { value: ElementCategory.Actinide, label: 'Actinide', color: '#e599f7' },
  { value: ElementCategory.Unknown, label: 'Unknown', color: '#868e96' }
];

export function getCategoryConfig(category: ElementCategory): CategoryConfig {
  return ELEMENT_CATEGORIES.find(c => c.value === category) ?? ELEMENT_CATEGORIES[ELEMENT_CATEGORIES.length - 1];
}
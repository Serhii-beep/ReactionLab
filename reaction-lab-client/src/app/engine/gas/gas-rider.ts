import { GasAirflow } from "./gas-airflow";

export interface GasRider {
    ride(seconds: number, airflow: GasAirflow): void;
    keep(seconds: number): void;
    resume(seconds: number): void;
    forget(): void;
}

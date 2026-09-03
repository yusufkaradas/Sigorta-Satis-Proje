import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PolicyCreate } from './policy-create';

describe('PolicyCreate', () => {
  let component: PolicyCreate;
  let fixture: ComponentFixture<PolicyCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PolicyCreate],
    }).compileComponents();

    fixture = TestBed.createComponent(PolicyCreate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
